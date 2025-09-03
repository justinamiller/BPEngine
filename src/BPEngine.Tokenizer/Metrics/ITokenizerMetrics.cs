using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Metrics
{
    public interface ITokenizerMetrics
    {
        // Called once per Encode/Decode call
        void OnEncodeCompleted(int inputChars, int outputTokens, double elapsedMs);
        void OnDecodeCompleted(int inputTokens, int outputChars, double elapsedMs);
    }
}
