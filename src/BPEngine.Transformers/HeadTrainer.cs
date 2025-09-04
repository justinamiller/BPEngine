using System.Runtime.InteropServices;

namespace BPEngine.Transformers
{
    /// <summary>
    /// Trains ONLY the output head Wout (dim x vocab) with Adam on cross-entropy.
    /// Frozen TinyTransformer backbone supplies hidden states.
    /// </summary>
    public sealed class HeadTrainer
    {
        private readonly TinyTransformer _model;
        private readonly int _D, _V;
        private readonly float[] _W;    // reference to model.Wout
        private readonly float[] _m;    // Adam m
        private readonly float[] _v;    // Adam v
        private int _t;

        public HeadTrainer(TinyTransformer model)
        {
            _model = model;
            _D = model.Dim;
            _V = model.VocabSize;
            _W = model.Wout;
            _m = new float[_W.Length];
            _v = new float[_W.Length];
            _t = 0;
        }

        public record TrainConfig(int BatchSize = 8, int SeqLen = 64, int Steps = 500, float LR = 1e-2f, float Beta1 = 0.9f, float Beta2 = 0.999f, float Eps = 1e-8f, float WeightDecay = 0.0f, int Seed = 123);

        /// <summary>
        /// Corpus is raw token IDs (already tokenized). We take sliding windows (SeqLen) with teacher forcing.
        /// </summary>
        public void Train(int[] corpusIds, TrainConfig cfg, Action<int, float>? onStep = null)
        {
            var rng = new Random(cfg.Seed);
            var dW = new float[_W.Length];

            for (int step = 1; step <= cfg.Steps; step++)
            {
                // Mini-batch gradients = 0
                Array.Clear(dW, 0, dW.Length);
                double totalLoss = 0.0;

                for (int b = 0; b < cfg.BatchSize; b++)
                {
                    // Sample a random contiguous span
                    if (corpusIds.Length < cfg.SeqLen + 1) throw new ArgumentException("corpus too small for chosen SeqLen");
                    int start = rng.Next(0, corpusIds.Length - (cfg.SeqLen + 1));
                    var input = corpusIds.AsSpan(start, cfg.SeqLen).ToArray();
                    var target = corpusIds[start + 1..start + 1 + cfg.SeqLen]; // next-token labels

                    // Forward: hidden [T,D]
                    var H = _model.ForwardHidden(input); // [T*D]

                    // Logits = H @ W (but we compute row by row to save memory)
                    // For each position t, logits[t,*] = H[t,:] x W
                    // Then softmax-cross-entropy and gradient dW += H[t]^T * (p - y_onehot)

                    for (int t = 0; t < cfg.SeqLen; t++)
                    {
                        // 1) logits = H[t,:] @ W  -> size V
                        var logits = new float[_V];
                        for (int v = 0; v < _V; v++)
                        {
                            float acc = 0f;
                            int wOff = v; // column-major interpretation needs careful mapping; we stored W as [D,V] row-major (dim fastest)
                            // We'll index as W[d * V + v]
                            int baseW = v; // column index in flattened [D,V] with stride V across d
                            for (int d = 0; d < _D; d++)
                                acc += H[t * _D + d] * _W[d * _V + v];
                            logits[v] = acc;
                        }

                        // 2) softmax
                        float max = logits.Max();
                        double sum = 0.0;
                        for (int v = 0; v < _V; v++) { var e = Math.Exp(logits[v] - max); logits[v] = (float)e; sum += e; }
                        for (int v = 0; v < _V; v++) logits[v] = (float)(logits[v] / sum);

                        // 3) loss = -log p[target]
                        int y = target[t];
                        float pY = Math.Clamp(logits[y], 1e-12f, 1f);
                        totalLoss += -Math.Log(pY);

                        // 4) gradient wrt logits: (p - y_onehot)
                        logits[y] -= 1f; // now logits holds (p - y)

                        // 5) dW += outer(H[t,:], (p - y))
                        for (int d = 0; d < _D; d++)
                        {
                            float hd = H[t * _D + d];
                            int rowOff = d * _V;
                            for (int v = 0; v < _V; v++)
                                dW[rowOff + v] += hd * logits[v];
                        }
                    }
                }

                // Average loss over tokens in batch
                float avgLoss = (float)(totalLoss / (cfg.BatchSize * cfg.SeqLen));

                // Adam update (optionally with weight decay)
                _t++;
                float lrT = cfg.LR * (float)(Math.Sqrt(1 - Math.Pow(cfg.Beta2, _t)) / (1 - Math.Pow(cfg.Beta1, _t)));

                for (int i = 0; i < _W.Length; i++)
                {
                    float g = dW[i] / cfg.BatchSize;
                    if (cfg.WeightDecay != 0) g += cfg.WeightDecay * _W[i];

                    _m[i] = cfg.Beta1 * _m[i] + (1 - cfg.Beta1) * g;
                    _v[i] = cfg.Beta2 * _v[i] + (1 - cfg.Beta2) * g * g;
                    _W[i] -= lrT * (_m[i] / (float)(Math.Sqrt(_v[i]) + cfg.Eps));
                }

                onStep?.Invoke(step, avgLoss);
            }
        }
    }
}
