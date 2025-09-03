using System.Text.Json;

namespace BPEngine.Models
{
    /// <summary>
    /// Simple n-gram (Markov) language model on token IDs.
    /// P(next | history) estimated from counts with add-k smoothing.
    /// Keys are string-joined histories for simplicity: "id1,id2,..."
    /// </summary>
    public sealed class NGramModel
    {
        public int Order { get; }
        public float AddK { get; }          // smoothing
        public int VocabSize { get; }       // for smoothing denom
        private readonly Dictionary<string, Dictionary<int, int>> _counts = new();
        private readonly Dictionary<string, int> _historyTotals = new();

        public NGramModel(int order, float addK, int vocabSize)
        {
            if (order < 1) throw new ArgumentOutOfRangeException(nameof(order));
            if (vocabSize <= 0) throw new ArgumentOutOfRangeException(nameof(vocabSize));
            Order = order;
            AddK = addK;
            VocabSize = vocabSize;
        }

        public void Observe(IReadOnlyList<int> tokens)
        {
            // Build n-grams for each position
            for (int i = 0; i < tokens.Count; i++)
            {
                int start = Math.Max(0, i - (Order - 1));
                var histSpan = tokens.Skip(start).Take(i - start).ToArray(); // length in [0..Order-1)
                var key = HistKey(histSpan);

                int next = tokens[i];
                if (!_counts.TryGetValue(key, out var nextMap))
                    _counts[key] = nextMap = new Dictionary<int, int>();
                nextMap.TryGetValue(next, out var c);
                nextMap[next] = c + 1;

                _historyTotals.TryGetValue(key, out var t);
                _historyTotals[key] = t + 1;
            }
        }

        /// <summary>Sample the next token ID from P(next|history) with temperature and top-k.</summary>
        public int SampleNext(ReadOnlySpan<int> history, Random rng, float temperature = 1.0f, int topK = 0)
        {
            // backoff: try longest suffix history to empty
            for (int h = Math.Min(Order - 1, history.Length); h >= 0; h--)
            {
                var key = HistKey(history.Slice(history.Length - h).ToArray());
                if (_counts.TryGetValue(key, out var nextMap) && nextMap.Count > 0)
                    return SampleFromCounts(nextMap, _historyTotals[key], rng, temperature, topK);
            }
            // If we have no counts at all, fallback to uniform
            return rng.Next(VocabSize);
        }

        // ---- persistence ----
        public void Save(string path)
        {
            var dto = new ModelDto
            {
                Order = Order,
                AddK = AddK,
                VocabSize = VocabSize,
                Counts = _counts,
                Totals = _historyTotals
            };
            var opts = new JsonSerializerOptions { WriteIndented = false };
            File.WriteAllText(path, JsonSerializer.Serialize(dto, opts));
        }

        public static NGramModel Load(string path)
        {
            var dto = JsonSerializer.Deserialize<ModelDto>(File.ReadAllText(path))
                      ?? throw new InvalidOperationException("Invalid n-gram model json");
            var m = new NGramModel(dto.Order, dto.AddK, dto.VocabSize);
            foreach (var (k, v) in dto.Counts) m._counts[k] = v;
            foreach (var (k, v) in dto.Totals) m._historyTotals[k] = v;
            return m;
        }

        // ---- helpers ----
        private static string HistKey(IReadOnlyList<int> ids)
            => ids.Count == 0 ? "" : string.Join(',', ids);

        private static int SampleFromCounts(Dictionary<int, int> counts, int total, Random rng, float temperature, int topK)
        {
            // convert counts -> smoothed probs -> tempered -> sample
            // collect candidates
            var items = counts.ToList();
            if (topK > 0 && items.Count > topK)
                items = items.OrderByDescending(kv => kv.Value).Take(topK).ToList();

            // smoothing: addK
            var denom = total + items.Count * 1.0f; // we only smooth over observed items for simplicity
            var probs = new List<(int id, double p)>(items.Count);
            foreach (var (id, c) in items)
            {
                double p = (c + 1.0 * 1 /*k=1 default via denom tweak*/) / denom;
                probs.Add((id, p));
            }

            // temperature
            if (temperature <= 0) temperature = 1e-6f;
            for (int i = 0; i < probs.Count; i++)
                probs[i] = (probs[i].id, Math.Pow(probs[i].p, 1.0 / temperature));

            // normalize
            double sum = probs.Sum(x => x.p);
            double r = rng.NextDouble() * sum;
            double acc = 0;
            foreach (var (id, p) in probs)
            {
                acc += p;
                if (r <= acc) return id;
            }
            return probs[^1].id;
        }

        private sealed class ModelDto
        {
            public int Order { get; set; }
            public float AddK { get; set; }
            public int VocabSize { get; set; }
            public Dictionary<string, Dictionary<int, int>> Counts { get; set; } = new();
            public Dictionary<string, int> Totals { get; set; } = new();
        }
    }
}
