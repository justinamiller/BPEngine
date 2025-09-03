using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public sealed class TokenizerDiagnostics
    {
        public int MergeCacheHits { get; internal set; }
        public int MergeCacheMisses { get; internal set; }
    }
}
