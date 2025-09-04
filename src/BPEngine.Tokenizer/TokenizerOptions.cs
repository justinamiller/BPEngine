using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public enum RegexPreset { Gpt2, Cl100k }

    /// <summary>
    /// Cross-tokenizer options. Allowed/disallowed specials match modern chat stacks.
    /// </summary>
    public sealed record TokenizerOptions(
        RegexPreset Regex = RegexPreset.Gpt2,
        ISet<string>? AllowedSpecial = null,
        ISet<string>? DisallowedSpecial = null,
        bool UseByteFallback = true,
            int MergeCacheCapacity = 50_000,
     int DecodeCacheCapacity = 50_000,
             int? MaxLength,
    bool ThrowOnUnknownId = true,
     int CacheCapacity= 0
    );
}
