using System.Diagnostics;

namespace BPEngine.Tokenizer.Metrics
{
    /// <summary>
    /// Wraps any ITokenizer and emits metrics via ITokenizerMetrics + ActivitySource.
    /// </summary>
    public sealed class InstrumentedTokenizer : ITokenizer
    {
        private readonly ITokenizer _inner;
        private readonly ITokenizerMetrics _metrics;

        public InstrumentedTokenizer(ITokenizer inner, ITokenizerMetrics? metrics = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _metrics = metrics ?? NullTokenizerMetrics.Instance;
        }

        public int[] Encode(string text)
        {
            using var activity = TokenizerTracing.Source.StartActivity("tokenizer.encode", ActivityKind.Internal);
            var sw = Stopwatch.StartNew();
            var ids = _inner.Encode(text);
            sw.Stop();

            _metrics.OnEncodeCompleted(inputChars: text?.Length ?? 0, outputTokens: ids.Length, elapsedMs: sw.Elapsed.TotalMilliseconds);

            activity?.SetTag("input.chars", text?.Length ?? 0);
            activity?.SetTag("output.tokens", ids.Length);
            activity?.SetTag("elapsed.ms", sw.Elapsed.TotalMilliseconds);
            return ids;
        }

        public string Decode(IEnumerable<int> ids)
        {
            // materialize to count tokens once
            var list = ids as int[] ?? ids.ToArray();

            using var activity = TokenizerTracing.Source.StartActivity("tokenizer.decode", ActivityKind.Internal);
            var sw = Stopwatch.StartNew();
            var s = _inner.Decode(list);
            sw.Stop();

            _metrics.OnDecodeCompleted(inputTokens: list.Length, outputChars: s?.Length ?? 0, elapsedMs: sw.Elapsed.TotalMilliseconds);

            activity?.SetTag("input.tokens", list.Length);
            activity?.SetTag("output.chars", s?.Length ?? 0);
            activity?.SetTag("elapsed.ms", sw.Elapsed.TotalMilliseconds);
            return s!;
        }
    }
}
