namespace BPEngine.Transformers
{
    public static class Sampling
    {
        public static int SampleLast(float[] logits, int vocab, float temperature = 1.0f, int topK = 0)
        {
            // Take last position
            int n = logits.Length;
            if (n < vocab) throw new ArgumentException("logits shorter than vocab width");
            var span = logits.AsSpan(n - vocab, vocab);

            // temperature
            if (temperature <= 0) temperature = 1e-6f;
            for (int i = 0; i < span.Length; i++) span[i] /= temperature;

            // top-k filter (keep topK; others to -inf)
            if (topK > 0 && topK < vocab)
            {
                // find threshold
                var idx = Enumerable.Range(0, vocab).OrderByDescending(i => span[i]).Take(topK).ToArray();
                var keep = new HashSet<int>(idx);
                for (int i = 0; i < vocab; i++)
                    if (!keep.Contains(i)) span[i] = float.NegativeInfinity;
            }

            // softmax + sample
            // (reuse a local copy to avoid modifying caller)
            var tmp = span.ToArray();
            // softmax
            float max = tmp.Max();
            double sum = 0;
            for (int i = 0; i < tmp.Length; i++) { var e = Math.Exp(tmp[i] - max); tmp[i] = (float)e; sum += e; }
            var r = new Random().NextDouble() * sum;
            double acc = 0;
            for (int i = 0; i < tmp.Length; i++)
            {
                acc += tmp[i];
                if (r <= acc) return i;
            }
            return vocab - 1;
        }
    }
}
