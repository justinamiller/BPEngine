using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    internal sealed class FeedForward
    {
        private readonly int _dim, _hidden;
        private readonly float[] _W1, _W2;

        public FeedForward(int dim, int hidden, Random rng)
        {
            _dim = dim; _hidden = hidden;
            _W1 = TinyMath.Rand(dim * hidden, 0.02f, rng);
            _W2 = TinyMath.Rand(hidden * dim, 0.02f, rng);
        }

        // x[T,D] -> y[T,D]
        public void Forward(float[] x, float[] y, int T)
        {
            var h = new float[T * _hidden];
            TinyMath.MatMul(x, _W1, h, T, _dim, _hidden);
            TinyMath.GeluInPlace(h.AsSpan());
            TinyMath.MatMul(h, _W2, y, T, _hidden, _dim);
        }
    }
}
