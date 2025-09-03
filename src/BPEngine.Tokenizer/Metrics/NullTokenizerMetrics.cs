using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Metrics
{
    // No-op for folks who don’t care
    public sealed class NullTokenizerMetrics : ITokenizerMetrics
    {
        public static readonly NullTokenizerMetrics Instance = new();
        private NullTokenizerMetrics() { }
        public void OnEncodeCompleted(int inputChars, int outputTokens, double elapsedMs) { }
        public void OnDecodeCompleted(int inputTokens, int outputChars, double elapsedMs) { }
    }
}
