using System;
using System.Linq;
using System.Collections.Generic;
namespace BPEngine.Transformers
{
    public static class Sampling
    {
        public static int SampleLast(float[] logits, int vocab, float temperature = 1.0f, int topK = 0)
        {
            // Take last position
            int n = logits.Length;
            if (n < vocab) throw new ArgumentException("logits shorter than vocab width");
            int baseIdx = n - vocab;
            var span = logits.AsSpan(baseIdx, vocab);

            // temperature
            if (temperature <= 0) temperature = 1e-6f;
            for (int i = 0; i < span.Length; i++) span[i] /= temperature;

            // top-k filter (keep topK; others to -inf)
            if (topK > 0 && topK < vocab)
            {
                // Build index array and sort by value WITHOUT touching 'span' in a lambda
                int[] idx = Enumerable.Range(0, vocab).ToArray();
                Array.Sort(idx, (a, b) => logits[baseIdx + b].CompareTo(logits[baseIdx + a])); // desc

                // mark all but topK as -inf
                var keep = new HashSet<int>(idx.Take(topK));
                for (int i = 0; i < vocab; i++)
                    if (!keep.Contains(i)) span[i] = float.NegativeInfinity;
            }

            // softmax + sample (use a local copy)
            var tmp = span.ToArray();

            // softmax (stable)
            float max = tmp.Max();
            double sum = 0;
            for (int i = 0; i < tmp.Length; i++)
            {
                double e = Math.Exp(tmp[i] - max);
                tmp[i] = (float)e;
                sum += e;
            }

            double r = new Random().NextDouble() * sum;
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