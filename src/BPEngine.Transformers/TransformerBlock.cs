using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    internal sealed class TransformerBlock
    {
        private readonly int _dim;
        private readonly MultiHeadAttention _attn;
        private readonly FeedForward _ff;
        private readonly float[] _ln1_g, _ln1_b, _ln2_g, _ln2_b;

        public TransformerBlock(int dim, int heads, int ffHidden, Random rng)
        {
            _dim = dim;
            _attn = new MultiHeadAttention(dim, heads, rng);
            _ff = new FeedForward(dim, ffHidden, rng);
            _ln1_g = Enumerable.Repeat(1f, dim).ToArray(); _ln1_b = new float[dim];
            _ln2_g = Enumerable.Repeat(1f, dim).ToArray(); _ln2_b = new float[dim];
        }

        public void Forward(float[] x, int T) // in-place residuals on x[T,D]
        {
            int D = _dim;
            var tmp = new float[T * D];

            // LN + Attn + Residual
            for (int t = 0; t < T; t++)
                TinyMath.LayerNormInPlace(x.AsSpan(t * D, D), _ln1_g, _ln1_b);
            _attn.Forward(x, tmp, T);
            for (int i = 0; i < tmp.Length; i++) x[i] += tmp[i];

            // LN + FF + Residual
            for (int t = 0; t < T; t++)
                TinyMath.LayerNormInPlace(x.AsSpan(t * D, D), _ln2_g, _ln2_b);
            _ff.Forward(x, tmp, T);
            for (int i = 0; i < tmp.Length; i++) x[i] += tmp[i];
        }
    }
}
