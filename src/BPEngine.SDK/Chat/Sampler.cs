using System;
using System.Linq;

namespace BPEngine.SDK.Chat
{
    /// <summary>
    /// Temperature / Top-K / Top-P sampling utilities.
    /// Use SampleFromProbs when you already have a probability vector,
    /// or ApplyTempTopKTopP to transform raw logits into a sample.
    /// </summary>
    internal static class Sampler
    {
        private static readonly Random _rng = new Random();

        public static int SampleFromProbs(ReadOnlySpan<float> probs)
        {
            // Assume probs are normalized (sum ~ 1).
            double r = _rng.NextDouble();
            double c = 0;
            for (int i = 0; i < probs.Length; i++)
            {
                c += probs[i];
                if (r <= c) return i;
            }
            return probs.Length - 1; // numeric edge
        }

        public static int SampleFromLogits(ReadOnlySpan<float> logits, float temperature = 1.0f, int topK = 0, float topP = 1.0f)
        {
            // 1) temperature
            var tmp = new float[logits.Length];
            if (temperature <= 0f) temperature = 1f;
            for (int i = 0; i < logits.Length; i++)
                tmp[i] = logits[i] / temperature;

            // 2) softmax
            float max = tmp.Max();
            double sum = 0;
            for (int i = 0; i < tmp.Length; i++)
            {
                var e = Math.Exp(tmp[i] - max);
                tmp[i] = (float)e;
                sum += e;
            }
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = (float)(tmp[i] / sum);

            // 3) top-k
            if (topK > 0 && topK < tmp.Length)
            {
                var idx = Enumerable.Range(0, tmp.Length)
                                    .OrderByDescending(i => tmp[i])
                                    .Take(topK).ToArray();
                var mask = new bool[tmp.Length];
                foreach (var i in idx) mask[i] = true;
                double z = 0;
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!mask[i]) tmp[i] = 0;
                    z += tmp[i];
                }
                if (z > 0)
                {
                    for (int i = 0; i < tmp.Length; i++) tmp[i] = (float)(tmp[i] / z);
                }
            }

            // 4) top-p (nucleus)
            if (topP < 0.9999f)
            {
                var order = Enumerable.Range(0, tmp.Length)
                                      .OrderByDescending(i => tmp[i])
                                      .ToArray();
                double acc = 0;
                var keep = new bool[tmp.Length];
                foreach (var i in order)
                {
                    keep[i] = true;
                    acc += tmp[i];
                    if (acc >= topP) break;
                }
                double z = 0;
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!keep[i]) tmp[i] = 0;
                    z += tmp[i];
                }
                if (z > 0)
                {
                    for (int i = 0; i < tmp.Length; i++) tmp[i] = (float)(tmp[i] / z);
                }
            }

            return SampleFromProbs(tmp);
        }
    }
}
