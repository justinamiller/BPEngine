using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Diagnostics.Metrics;

namespace BPEngine.Tokenizer.Metrics
{
    // EventSource: view via PerfView/ETW or dotnet-trace on Windows
    [EventSource(Name = "BPEngine.Tokenizer")]
    public sealed class TokenizerEventSource : EventSource
    {
        public static readonly TokenizerEventSource Log = new();

        [Event(1, Level = EventLevel.Informational)]
        public void EncodeCompleted(int inputChars, int outputTokens, double elapsedMs) => WriteEvent(1, inputChars, outputTokens, elapsedMs);

        [Event(2, Level = EventLevel.Informational)]
        public void DecodeCompleted(int inputTokens, int outputChars, double elapsedMs) => WriteEvent(2, inputTokens, outputChars, elapsedMs);
    }

    // Metrics (OpenTelemetry-friendly): counters & histograms
    public sealed class TokenizerMeters : IDisposable
    {
        public static readonly string MeterName = "BPEngine.Tokenizer";
        private readonly Meter _meter = new(MeterName, "1.0.0");

        // Instruments
        public Counter<long> TokensProcessed { get; }
        public Histogram<double> EncodeDurationMs { get; }
        public Histogram<double> DecodeDurationMs { get; }

        public TokenizerMeters()
        {
            TokensProcessed = _meter.CreateCounter<long>("tokens_processed", unit: "tokens", description: "Total tokens processed (encode/decode)");
            EncodeDurationMs = _meter.CreateHistogram<double>("encode_duration_ms", unit: "ms", description: "Encode wall time (ms)");
            DecodeDurationMs = _meter.CreateHistogram<double>("decode_duration_ms", unit: "ms", description: "Decode wall time (ms)");
        }

        public void Dispose() => _meter?.Dispose();
    }

    // ActivitySource for tracing (shows up in OpenTelemetry pipelines)
    public static class TokenizerTracing
    {
        public static readonly ActivitySource Source = new("BPEngine.Tokenizer", "1.0.0");
    }

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
