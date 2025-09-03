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
}
