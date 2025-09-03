using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    internal sealed class MultiHeadAttention
    {
        private readonly int _dim, _heads, _headDim;
        private readonly float[] _Wq, _Wk, _Wv, _Wo; // projections

        public MultiHeadAttention(int dim, int heads, Random rng)
        {
            _dim = dim; _heads = heads; _headDim = dim / heads;
            _Wq = TinyMath.Rand(dim * dim, 0.02f, rng);
            _Wk = TinyMath.Rand(dim * dim, 0.02f, rng);
            _Wv = TinyMath.Rand(dim * dim, 0.02f, rng);
            _Wo = TinyMath.Rand(dim * dim, 0.02f, rng);
        }

        // x: [T,D] -> out: [T,D], causal self-attention
        public void Forward(float[] x, float[] output, int T)
        {
            int D = _dim;
            Array.Clear(output, 0, output.Length);

            // Project Q,K,V (flattened [T,D])
            var Q = new float[T * D]; var K = new float[T * D]; var V = new float[T * D];
            TinyMath.MatMul(x, _Wq, Q, T, D, D);
            TinyMath.MatMul(x, _Wk, K, T, D, D);
            TinyMath.MatMul(x, _Wv, V, T, D, D);

            // For each head
            var headOut = new float[T * _headDim];
            var scores = new float[T * T];
            for (int h = 0; h < _heads; h++)
            {
                int hOff = h * _headDim;

                // Compute scores = Q_h * K_h^T / sqrt(d)
                for (int i = 0; i < T; i++)
                {
                    for (int j = 0; j < T; j++)
                    {
                        float s = 0f;
                        int qi = i * D + hOff;
                        int kj = j * D + hOff;
                        for (int d = 0; d < _headDim; d++)
                            s += Q[qi + d] * K[kj + d];
                        // scale
                        s /= (float)Math.Sqrt(_headDim);
                        // causal mask: j > i => -inf
                        if (j > i) s = float.NegativeInfinity;
                        scores[i * T + j] = s;
                    }
                    TinyMath.SoftmaxInPlace(scores.AsSpan(i * T, T));
                }

                // Weighted sum of V_h
                Array.Clear(headOut, 0, headOut.Length);
                for (int i = 0; i < T; i++)
                {
                    int qi = i * _headDim;
                    for (int j = 0; j < T; j++)
                    {
                        float w = scores[i * T + j];
                        int vj = j * D + hOff;
                        for (int d = 0; d < _headDim; d++)
                            headOut[qi + d] += w * V[vj + d];
                    }
                }

                // Add into output slot for this head
                for (int t = 0; t < T; t++)
                {
                    int outOff = t * D + hOff;
                    Array.Copy(headOut, t * _headDim, output, outOff, _headDim);
                }
            }

            // Final projection
            var y = new float[T * D];
            TinyMath.MatMul(output, _Wo, y, T, D, D);
            Array.Copy(y, output, y.Length);
        }
    }
}
