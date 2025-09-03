using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public sealed class TokenizerOptions
    {
        public int? MaxLength { get; init; }
        public bool ThrowOnUnknownId { get; init; } = true;
        public int CacheCapacity { get; init; } = 0;
        public int MergeCacheCapacity { get; init; } = 50_000;
        public int DecodeCacheCapacity { get; init; } = 50_000;
    }
}
