using BPEngine.SDK.Chat;
using BPEngine.Tokenizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.SDK
{
    /// <summary>
    /// Options for ChatAsync generation.
    /// </summary>
    public sealed record ChatOptions(
        bool UseRag = true,
        int MaxNewTokens = 200,
        float Temperature = 0.8f,
        int TopK = 50,
        float TopP = 0.95f
    );

    public sealed partial class BPEngineSdk
    {
        // ---------- Bigram model (lazy, tiny and CPU-only) ----------
        private Dictionary<int, Dictionary<int, int>>? _bigram;
        private readonly object _bigramLock = new();

        /// <summary>
        /// Generate assistant text for a multi-turn chat using a simple bigram model and optional RAG context.
        /// Also supports minimal function calling if the model emits {"tool":"name","args":{...}}.
        /// </summary>
        public async Task<string> ChatAsync(ChatSession session, ChatOptions? options = null)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            options ??= new ChatOptions();

            // 1) Build a simple prompt
            var prompt = BuildPromptText(session);

            // 2) Optional RAG snippets (uses QueryRag already available in SDK)
            if (options.UseRag)
            {
                var lastUser = session.Turns.LastOrDefault(t => t.role == "user").content ?? string.Empty;
                var rag = QueryRag(lastUser, k: 3);
                if (rag.Hits.Count > 0)
                {
                    var sb = new StringBuilder(prompt.Length + 512);
                    sb.AppendLine(prompt);
                    sb.AppendLine();
                    sb.AppendLine("[Context]");
                    foreach (var h in rag.Hits)
                    {
                        if (!string.IsNullOrWhiteSpace(h.Snippet))
                            sb.AppendLine("• " + h.Snippet.Replace("\r", " ").Replace('\n', ' '));
                    }
                    prompt = sb.ToString();
                }
            }

            // 3) Encode
            var seed = _tok.Encode(prompt);

            // 4) Ensure a tiny bigram model exists
            EnsureBigramBuilt();

            // 5) Generate with bigram sampling
            var gen = GenerateWithBigram(seed, options.MaxNewTokens, options.Temperature, options.TopK, options.TopP);

            // 6) Decode the tail
            var tail = gen.Skip(seed.Length);
            var text = _tok.Decode(tail).Trim();

            // 7) Optional: detect a tool call and execute
            var tc = ToolRegistry.TryParseToolJson(text);
            if (tc is not null && ToolRegistry.TryGet(tc.Value.tool, out var handler))
            {
                var toolResult = await handler(tc.Value.args);
                // Append tool result to the conversation in a simple way
                return $"[Tool:{tc.Value.tool}]\n{toolResult}";
            }

            return text;
        }

        private string BuildPromptText(ChatSession session)
        {
            // Simple readable template
            var sb = new StringBuilder(512);
            foreach (var (role, content) in session.Turns)
            {
                var line = content?.Trim() ?? string.Empty;
                if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"[System] {line}");
                else if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"User: {line}");
                else if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"Assistant: {line}");
            }
            sb.Append("Assistant: ");
            return sb.ToString();
        }

        private void EnsureBigramBuilt()
        {
            if (_bigram != null) return;
            lock (_bigramLock)
            {
                if (_bigram != null) return;

                // Build from the corpus if available; otherwise fall back to an empty model
                var map = new Dictionary<int, Dictionary<int, int>>();
                try
                {
                    if (!string.IsNullOrWhiteSpace(_opt.CorpusPath) && File.Exists(_opt.CorpusPath!))
                    {
                        var text = File.ReadAllText(_opt.CorpusPath!);
                        var ids = _tok.Encode(text);
                        for (int i = 0; i < ids.Length - 1; i++)
                        {
                            var a = ids[i];
                            var b = ids[i + 1];
                            if (!map.TryGetValue(a, out var next)) map[a] = next = new Dictionary<int, int>();
                            next.TryGetValue(b, out var c);
                            next[b] = c + 1;
                        }
                    }
                }
                catch
                {
                    // ignore build errors, keep map empty
                }

                _bigram = map;
            }
        }

        private int[] GenerateWithBigram(int[] seed, int maxNew, float temperature, int topK, float topP)
        {
            var outIds = new List<int>(seed.Length + Math.Max(8, maxNew));
            outIds.AddRange(seed);

            for (int s = 0; s < maxNew; s++)
            {
                var prev = outIds[^1];

                // build distribution for next token
                float[] probs;
                if (_bigram != null && _bigram.TryGetValue(prev, out var next) && next.Count > 0)
                {
                    // convert counts to probs
                    var keys = next.Keys.ToArray();
                    var vals = keys.Select(k => (float)next[k]).ToArray();
                    var sum = vals.Sum();
                    if (sum <= 0) sum = 1;
                    for (int i = 0; i < vals.Length; i++) vals[i] /= sum;

                    // we need the full-vocab probs to use generic sampler; create a sparse projection
                    // To keep it tiny, sample directly from the sparse distribution:
                    int idxSparse = SampleSparse(vals, temperature, topK, topP);
                    var nextId = keys[idxSparse];
                    outIds.Add(nextId);
                }
                else
                {
                    // fallback: repeat previous or end
                    outIds.Add(prev);
                }
            }
            return outIds.ToArray();

            static int SampleSparse(float[] probs, float temperature, int topK, float topP)
            {
                // Apply temperature
                if (temperature != 1f && temperature > 0f)
                {
                    float max = probs.Max();
                    double sum = 0;
                    for (int i = 0; i < probs.Length; i++)
                    {
                        // work in log-space via small trick: treat probs as logits by ln, but probabilities can be zero; clamp
                        var p = Math.Clamp(probs[i], 1e-12f, 1f);
                        var logit = Math.Log(p);
                        var e = Math.Exp((logit) / temperature);
                        probs[i] = (float)e;
                        sum += e;
                    }
                    for (int i = 0; i < probs.Length; i++)
                        probs[i] = (float)(probs[i] / sum);
                }

                // top-k
                if (topK > 0 && topK < probs.Length)
                {
                    var order = Enumerable.Range(0, probs.Length).OrderByDescending(i => probs[i]).Take(topK).ToArray();
                    var mask = new bool[probs.Length];
                    foreach (var i in order) mask[i] = true;
                    double z = 0;
                    for (int i = 0; i < probs.Length; i++)
                    {
                        if (!mask[i]) probs[i] = 0;
                        z += probs[i];
                    }
                    if (z > 0)
                        for (int i = 0; i < probs.Length; i++) probs[i] = (float)(probs[i] / z);
                }

                // top-p
                if (topP < 0.9999f)
                {
                    var order = Enumerable.Range(0, probs.Length).OrderByDescending(i => probs[i]).ToArray();
                    double acc = 0;
                    var keep = new bool[probs.Length];
                    foreach (var i in order)
                    {
                        keep[i] = true;
                        acc += probs[i];
                        if (acc >= topP) break;
                    }
                    double z = 0;
                    for (int i = 0; i < probs.Length; i++)
                    {
                        if (!keep[i]) probs[i] = 0;
                        z += probs[i];
                    }
                    if (z > 0)
                        for (int i = 0; i < probs.Length; i++) probs[i] = (float)(probs[i] / z);
                }

                // sample
                double r = _rng.NextDouble();
                double c = 0;
                for (int i = 0; i < probs.Length; i++)
                {
                    c += probs[i];
                    if (r <= c) return i;
                }
                return probs.Length - 1;
            }
        }
    }
}
