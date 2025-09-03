
namespace BPEngine.Trainer
{
    public sealed class TrainerOptions
    {
        /// <summary>Total vocabulary size including special tokens. Typical 50k.</summary>
        public int VocabSize { get; init; } = 5000;

        /// <summary>Maximum number of merges to learn; if set, overrides VocabSize.</summary>
        public int? MaxMerges { get; init; }

        /// <summary>Reserved special tokens and their fixed IDs.</summary>
        public IReadOnlyDictionary<string,int> Specials { get; init; } = new Dictionary<string,int>();

        /// <summary>Minimum pair frequency to be considered for merges.</summary>
        public int MinPairFrequency { get; init; } = 2;

        /// <summary>If true, use GPT‑style regex pretokenizer; otherwise split on whitespace.</summary>
        public bool UseGptRegexPretokenizer { get; init; } = true;
    }
}
