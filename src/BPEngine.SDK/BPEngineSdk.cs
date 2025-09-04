using System.Text;
using BPEngine.Tokenizer;
using BPEngine.Generation;
using BPEngine.RAG;
using BPEngine.Trainer;
using BPEngine.Transformers;

namespace BPEngine.SDK;

public sealed class BPEngineSdk : IAsyncDisposable
{
    private readonly BpeSdkOptions _opt;
    private readonly ITokenizer _tok;
    private readonly Func<int, string> _idToPiece;
    private readonly TfIdfIndex? _rag;
    private readonly JsonConstrainedDecoder _jsonConstraint = new();

    private BPEngineSdk(BpeSdkOptions opt, ITokenizer tok, Func<int, string> idToPiece, TfIdfIndex? rag)
    { _opt = opt; _tok = tok; _idToPiece = idToPiece; _rag = rag; }

    public static BPEngineSdkBuilder Builder() => new();

    // Builder wrapper so consumers can do BPEngineSdk.Builder()...
    public sealed class BPEngineSdkBuilder
    {
        private readonly BpeSdkOptions.Builder _b = BpeSdkOptions.Create();
        public BPEngineSdkBuilder UsePresetGpt2(string mergesPath, string? vocabPath = null) { _b.UsePresetGpt2(mergesPath, vocabPath); return this; }
        public BPEngineSdkBuilder UsePresetCl100k(string ranksPath, string? specialsPath = null) { _b.UsePresetCl100k(ranksPath, specialsPath); return this; }
        public BPEngineSdkBuilder EnableRag(string corpusPath) { _b.EnableRag(corpusPath); return this; }
        public BPEngineSdkBuilder WithTransformerDefaults(int dim = 64, int heads = 2, int layers = 2, int maxSeq = 128) { _b.WithTransformerDefaults(dim, heads, layers, maxSeq); return this; }
        public BPEngineSdkBuilder WithPerf(bool collect = true) { _b.WithPerf(collect); return this; }

        public BPEngineSdk Build()
        {
            var o = _b.Build();

            // 1) Tokenizer
            ITokenizer tok;
            Func<int, string> id2piece;
            if (o.Preset.Equals("cl100k", StringComparison.OrdinalIgnoreCase))
            {
                tok = TokenizerFactory.CreateCl100k(o.RanksPath ?? throw new ArgumentException("ranks required"));
                // Build reverse map once for constrained decoding & debug
                var model = (tok as TikTokenTokenizer) ?? throw new InvalidOperationException("Expected TikTokenTokenizer");
                // You should expose a GetPiece(id) in TikTokenTokenizer; for now, simple closure:
                id2piece = id => model.Decode(new[] { id }); // replace with O(1) map in your impl
            }
            else
            {
                Dictionary<string, int>? vocab = null;
                if (!string.IsNullOrWhiteSpace(o.VocabPath))
                    vocab = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(o.VocabPath!));
                tok = TokenizerFactory.CreateGpt2(o.MergesPath ?? throw new ArgumentException("merges required"), vocab);
                // build reverse map for gpt2 path
                var rev = vocab is null ? new Dictionary<int, string>() : vocab.ToDictionary(kv => kv.Value, kv => kv.Key);
                id2piece = id => rev.TryGetValue(id, out var s) ? s : "";
            }

            // 2) RAG
            TfIdfIndex? rag = null;
            if (o.EnableRag && !string.IsNullOrWhiteSpace(o.CorpusPath))
            {
                rag = new TfIdfIndex();
                foreach (var doc in File.ReadAllText(o.CorpusPath!).Split("\n\n"))
                    rag.AddDocument(doc.Trim());
                rag.Fit();
            }

            return new BPEngineSdk(o, tok, id2piece, rag);
        }
    }

    // ---- High-level ops ----

    public ValueTask<EncodeResult> EncodeAsync(string text)
        => ValueTask.FromResult(new EncodeResult(_tok.Encode(text)));

    public ValueTask<DecodeResult> DecodeAsync(IEnumerable<int> ids)
        => ValueTask.FromResult(new DecodeResult(_tok.Decode(ids)));

    public RagResult QueryRag(string query, int k = 5)
    {
        if (_rag is null) return new RagResult(Array.Empty<RagHit>());
        var hits = _rag.Query(query, k);
        var docs = File.ReadAllText(_opt.CorpusPath!).Split("\n\n");
        var list = hits.Select(h => new RagHit(h.Doc, h.Score, docs[h.Doc])).ToList();
        return new RagResult(list);
    }

    public async Task<GenerateResult> GenerateJsonAsync(string prompt, string schemaJson, bool useRag = true, int maxNew = 128)
    {
        // This is a placeholder: wire to your existing generator loop and call ConstrainedSampler.ApplyConstraint(...)
        // Use _jsonConstraint to ensure outputs remain valid JSON by masking logits.
        // For the façade, we return the prompt + RAG snippets to show the flow.
        var sb = new StringBuilder();
        if (useRag)
        {
            var rr = QueryRag(prompt, 3);
            sb.AppendLine("/* context */");
            foreach (var h in rr.Hits) sb.AppendLine(h.Snippet);
        }
        sb.AppendLine("/* schema */");
        sb.AppendLine(schemaJson);
        sb.AppendLine("/* draft */");
        sb.AppendLine("{\"summary\":\"...\",\"nextSteps\":[\"...\",\"...\"]}");
        return await Task.FromResult(new GenerateResult(Text: sb.ToString(), Json: null));
    }

    public async Task<TrainResult> TrainTokenizerAsync(string corpusPath, string outDir, int vocabSize = 32000, int minPair = 2)
    {
        Directory.CreateDirectory(outDir);
        var mergesOut = Path.Combine(outDir, "merges.txt");
        var vocabOut = Path.Combine(outDir, "vocab.json");

        // Call into your Trainer APIs (pseudo—replace with your real class names)
        var trainer = new BpeTrainer(vocabSize: vocabSize, minPair: minPair);
        await trainer.TrainAsync(corpusPath, mergesOut, vocabOut);

        return new TrainResult(mergesOut, vocabOut);
    }

    public ValueTask<AnalyzeReport> AnalyzeAsync(string corpusPath, int top = 20)
    {
        // Call your analyzer; placeholder summary here
        var text = File.ReadAllText(corpusPath);
        var ids = _tok.Encode(text);
        var total = ids.Length;
        var topTokens = new List<(string, int)>();
        return ValueTask.FromResult(new AnalyzeReport(total, topTokens));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
