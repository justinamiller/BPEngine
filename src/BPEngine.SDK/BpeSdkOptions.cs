namespace BPEngine.SDK;

/// <summary>
/// Configuration options for building a BPEngineSdk instance.
/// Use <see cref="BpeSdkOptions.Builder"/> to construct an options object fluently.
/// </summary>
public sealed class BpeSdkOptions
{
    /// <summary>
    /// Path to a GPT-2/3 style <c>merges.txt</c> file.
    /// Only required if <see cref="Preset"/> is "gpt2".
    /// </summary>
    public string? MergesPath { get; set; }

    /// <summary>
    /// Path to a GPT-2/3 style <c>vocab.json</c> file.
    /// Optional, but recommended to ensure stable IDs for encode/decode.
    /// </summary>
    public string? VocabPath { get; set; }

    /// <summary>
    /// Path to a GPT-3.5/4 style <c>ranks.json</c> file.
    /// Only required if <see cref="Preset"/> is "cl100k".
    /// </summary>
    public string? RanksPath { get; set; }

    /// <summary>
    /// Path to an optional <c>specials.json</c> file defining special tokens.
    /// Only used with the "cl100k" preset.
    /// </summary>
    public string? SpecialsPath { get; set; }

    /// <summary>
    /// Tokenizer preset to use:
    /// <c>"gpt2"</c> for GPT-2/3 byte-level BPE (default),
    /// <c>"cl100k"</c> for GPT-3.5/4 mergeable ranks.
    /// </summary>
    public string Preset { get; set; } = "gpt2";

    /// <summary>
    /// Path to a text corpus for training or retrieval-augmented generation.
    /// If <see cref="EnableRag"/> is true, the corpus is indexed for retrieval.
    /// </summary>
    public string? CorpusPath { get; set; }

    /// <summary>
    /// Enable retrieval-augmented generation (RAG).
    /// If true, the SDK will build or load a TF-IDF index from <see cref="CorpusPath"/>.
    /// </summary>
    public bool EnableRag { get; set; }

    /// <summary>
    /// Embedding/Transformer dimension size for tiny transformer demos or head training.
    /// Default = 64.
    /// </summary>
    public int Dim { get; set; } = 64;

    /// <summary>
    /// Number of attention heads for the tiny transformer.
    /// Default = 2.
    /// </summary>
    public int Heads { get; set; } = 2;

    /// <summary>
    /// Number of transformer layers for the tiny transformer.
    /// Default = 2.
    /// </summary>
    public int Layers { get; set; } = 2;

    /// <summary>
    /// Maximum sequence length the tiny transformer should handle.
    /// Default = 128.
    /// </summary>
    public int MaxSeq { get; set; } = 128;

    /// <summary>
    /// If true, the SDK collects performance metrics (tokens/sec, cache hits, etc.).
    /// </summary>
    public bool CollectPerf { get; set; }

    /// <summary>
    /// Creates a new fluent builder for <see cref="BpeSdkOptions"/>.
    /// </summary>
    public static Builder Create() => new();

    /// <summary>
    /// Fluent builder for <see cref="BpeSdkOptions"/>.
    /// </summary>
    public sealed class Builder
    {
        private readonly BpeSdkOptions _o = new();

        /// <summary>
        /// Configure the SDK to use GPT-2/3 style BPE with a merges file and optional vocab.
        /// </summary>
        public Builder UsePresetGpt2(string mergesPath, string? vocabPath = null)
        { _o.Preset = "gpt2"; _o.MergesPath = mergesPath; _o.VocabPath = vocabPath; return this; }

        /// <summary>
        /// Configure the SDK to use GPT-3.5/4 style cl100k tokenizer with a ranks file and optional specials.
        /// </summary>
        public Builder UsePresetCl100k(string ranksPath, string? specialsPath = null)
        { _o.Preset = "cl100k"; _o.RanksPath = ranksPath; _o.SpecialsPath = specialsPath; return this; }

        /// <summary>
        /// Enable retrieval-augmented generation by indexing the given corpus file.
        /// </summary>
        public Builder EnableRag(string corpusPath)
        { _o.EnableRag = true; _o.CorpusPath = corpusPath; return this; }

        /// <summary>
        /// Configure tiny transformer defaults (dimension, heads, layers, max sequence length).
        /// </summary>
        public Builder WithTransformerDefaults(int dim = 64, int heads = 2, int layers = 2, int maxSeq = 128)
        {
            _o.Dim = dim; 
            _o.Heads = heads;
            _o.Layers = layers;
            _o.MaxSeq = maxSeq; 
            return this; 
        }

        /// <summary>
        /// Enable or disable performance metric collection.
        /// </summary>
        public Builder WithPerf(bool collect = true) { _o.CollectPerf = collect; return this; }

        /// <summary>
        /// Finalize and build the options object.
        /// </summary>
        public BpeSdkOptions Build() => _o;
    }
}
