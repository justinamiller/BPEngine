using System.Text;
using BPEngine.Tokenizer;
using BPEngine.Generation;
using BPEngine.RAG;
using BPEngine.Trainer;
using BPEngine.Transformers;
using BPEngine.Tokenizer.Core;

namespace BPEngine.SDK;

public sealed class BPEngineSdk : IAsyncDisposable
{
    private readonly BpeSdkOptions _opt;
    private readonly ITokenizer _tok;
    private readonly Func<int, string> _idToPiece;
    private readonly TfIdfIndex? _rag;
    private readonly JsonConstrainedDecoder _jsonConstraint = new();
    private TfIdfIndex? _ragIndex;
    private string[] _ragCorpusDocs = Array.Empty<string>();

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
        var idx = _ragIndex ?? _rag; // loaded or built-at-start
        if (idx is null) return new RagResult(Array.Empty<RagHit>());
        var hits = idx.Query(query, k);
        var docs = _ragCorpusDocs.Length > 0
            ? _ragCorpusDocs
            : (!string.IsNullOrWhiteSpace(_opt.CorpusPath)
                ? File.ReadAllText(_opt.CorpusPath!).Split("\n\n")
                : Array.Empty<string>());

        var list = hits.Select(h =>
            new RagHit(h.Doc, h.Score, (h.Doc >= 0 && h.Doc < docs.Length) ? docs[h.Doc] : string.Empty)).ToList();
        return new RagResult(list);
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
        var s = TokenStatsAnalyzer.Analyze(_tok, corpusPath, top);
        // Flatten bigrams into the report if/when you add them to the DTO; for now return top tokens.
        return ValueTask.FromResult(new AnalyzeReport(
            TotalTokens: s.TotalTokens,
            TopTokens: s.TopTokens
        ));
    }


    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async Task BuildRagAsync(string corpusPath, string indexPath)
    {
        var idx = new TfIdfIndex();
        foreach (var doc in File.ReadAllText(corpusPath).Split("\n\n"))
            idx.AddDocument(doc.Trim());
        idx.Fit();
        idx.Save(indexPath);
        await Task.CompletedTask;
    }

    public async Task LoadRagAsync(string indexPath, string? corpusPathForSnippets = null)
    {
        var idx = TfIdfIndex.Load(indexPath);
        _ragIndex = idx;
        _ragCorpusDocs = corpusPathForSnippets != null
            ? File.ReadAllText(corpusPathForSnippets).Split("\n\n")
            : Array.Empty<string>();
        await Task.CompletedTask;
    }

    public async Task<GenerateResult> GenerateJsonAsync(string prompt, string schemaJson, bool useRag = true, int maxNew = 128)
    {
        // 1) Retrieve a few relevant snippets (optional)
        var contextSnippets = new List<string>();
        if (useRag)
        {
            var rr = QueryRag(prompt, 3);
            foreach (var h in rr.Hits) contextSnippets.Add(h.Snippet);
        }

        // 2) Ultra-simple extraction heuristic: summary = first relevant line; nextSteps = bullet-like lines.
        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var joined = string.Join("\n", contextSnippets);
        var lines = joined.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? summary = lines.FirstOrDefault(l => l.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                          ?? lines.FirstOrDefault(l => l.StartsWith("Impact:", StringComparison.OrdinalIgnoreCase))
                          ?? lines.FirstOrDefault();

        var steps = lines.Where(l =>
            l.StartsWith("Next Steps:", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("- ") || l.StartsWith("* ") || l.Contains(" • "))
            .ToArray();

        if (summary != null) extracted["summary"] = summary.Replace("Summary:", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (steps.Length > 0) extracted["nextSteps"] = string.Join("\n", steps.Select(s => s.TrimStart('-', '*', ' ', '•').Trim()));

        // 3) Conform to schema & validate (always-valid JSON output)
        var obj = JsonSchemaConformer.Conform(schemaJson, extracted);
        if (!JsonSchemaConformer.TryValidate(schemaJson, obj, out var err))
        {
            // Auto-repair fallback: ensure required keys exist (Conform already did); attach error.
            obj["note"] = $"auto_repaired: {err}";
        }

        // 4) Return both the pretty JSON and a plain-text “explanation” if desired
        var jsonText = obj.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        return await Task.FromResult(new GenerateResult(
            Text: $"Prompt: {prompt}\n\n{(useRag ? "RAG used.\n" : "")}JSON follows.",
            Json: jsonText
        ));
    }


}
