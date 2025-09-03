using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Metrics
{
    // Concrete metrics sink that fans out to EventSource + Meters (+ optional ILogger)
    public sealed class StandardTokenizerMetrics : ITokenizerMetrics, IDisposable
    {
        private readonly TokenizerEventSource _events = TokenizerEventSource.Log;
        private readonly TokenizerMeters _meters = new();
        private readonly Action<string>? _loggerInfo; // optional delegate if host wants logs

        public StandardTokenizerMetrics(Action<string>? loggerInfo = null)
        {
            _loggerInfo = loggerInfo;
        }

        public void OnEncodeCompleted(int inputChars, int outputTokens, double elapsedMs)
        {
            _events.EncodeCompleted(inputChars, outputTokens, elapsedMs);
            _meters.TokensProcessed.Add(outputTokens);
            _meters.EncodeDurationMs.Record(elapsedMs);

            _loggerInfo?.Invoke($"Encode: chars={inputChars} tokens={outputTokens} {outputTokens / (elapsedMs / 1000.0):0.0} tok/s in {elapsedMs:0.0}ms");
        }

        public void OnDecodeCompleted(int inputTokens, int outputChars, double elapsedMs)
        {
            _events.DecodeCompleted(inputTokens, outputChars, elapsedMs);
            _meters.TokensProcessed.Add(inputTokens);
            _meters.DecodeDurationMs.Record(elapsedMs);

            _loggerInfo?.Invoke($"Decode: tokens={inputTokens} chars={outputChars} {inputTokens / (elapsedMs / 1000.0):0.0} tok/s in {elapsedMs:0.0}ms");
        }

        public void Dispose() => _meters.Dispose();
    }
}
