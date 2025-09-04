namespace BPEngine.Tokenizer
{
    /// <summary>
    /// Presets for regex token splitting.
    /// GPT-2 and cl100k follow slightly different patterns.
    /// </summary>
    public enum RegexPreset
    {
        Gpt2,
        Cl100k
    }

    /// <summary>
    /// Cross-tokenizer options. 
    /// Controls regex mode, special token handling, caching, and fallback behavior.
    /// </summary>
    public sealed record TokenizerOptions(
        /// <summary>Which regex preset to use (GPT-2 default).</summary>
        RegexPreset Regex = RegexPreset.Gpt2,

        /// <summary>Special tokens that are allowed to pass through without error.</summary>
        ISet<string>? AllowedSpecial = null,

        /// <summary>Special tokens that should throw if encountered (stronger safety).</summary>
        ISet<string>? DisallowedSpecial = null,

        /// <summary>If true, unknown bytes fall back to raw byte-to-unicode mapping.</summary>
        bool UseByteFallback = true,

        /// <summary>Capacity of merge application LRU cache (default 50k entries).</summary>
        int MergeCacheCapacity = 50_000,

        /// <summary>Capacity of decode ID→piece cache (default 50k entries).</summary>
        int DecodeCacheCapacity = 50_000,

        /// <summary>If true, throw when an unknown token ID is decoded.</summary>
        bool ThrowOnUnknownId = true,

        /// <summary>General-purpose cache capacity (0 = disabled).</summary>
        int CacheCapacity = 0,

        /// <summary>Optional maximum sequence length (null = unlimited).</summary>
        int? MaxLength = null
    );
}
