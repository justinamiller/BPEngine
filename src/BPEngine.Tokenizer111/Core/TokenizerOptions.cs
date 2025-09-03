using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public sealed class TokenizerOptions
    {
        public int? MaxLength { get; init; }
        public bool ThrowOnUnknownId { get; init; } = true;
        public int CacheCapacity { get; init; } = 0;
    }
}
