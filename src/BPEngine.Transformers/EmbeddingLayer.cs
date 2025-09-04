using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    internal sealed class EmbeddingLayer
    {
        private readonly int _vocab, _dim;
        private readonly float[] _table;

        public EmbeddingLayer(int vocab, int dim, Random rng)
        {
            _vocab = vocab; _dim = dim;
            _table = TinyMath.Rand(vocab * dim, scale: 0.02f, rng);
        }

        // x[t * dim + d] = E[token_t, d]
        public void Forward(int[] tokens, float[] output) // output len = T*D
        {
            int T = tokens.Length;
            for (int t = 0; t < T; t++)
            {
                int tok = Math.Clamp(tokens[t], 0, _vocab - 1);
                Array.Copy(_table, tok * _dim, output, t * _dim, _dim);
            }
        }
    }
}
