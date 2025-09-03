namespace BPEngine.Transformers
{
    internal sealed class PositionalEncoding
    {
        private readonly int _maxT, _dim;
        private readonly float[] _table; // [maxT, dim]

        public PositionalEncoding(int maxT, int dim)
        {
            _maxT = maxT; _dim = dim;
            _table = new float[maxT * dim];
            for (int pos = 0; pos < maxT; pos++)
            {
                for (int i = 0; i < dim; i += 2)
                {
                    double div = Math.Pow(10000, (double)i / dim);
                    _table[pos * dim + i] = (float)Math.Sin(pos / div);
                    if (i + 1 < dim)
                        _table[pos * dim + i + 1] = (float)Math.Cos(pos / div);
                }
            }
        }

        public void AddInPlace(float[] x, int T) // x[T,D]
        {
            int D = _dim;
            for (int t = 0; t < T; t++)
            {
                int off = t * D;
                for (int d = 0; d < D; d++) x[off + d] += _table[t * D + d];
            }
        }
    }
}
