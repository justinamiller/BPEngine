using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    public sealed class TinyTransformer
    {
        private readonly int _vocab, _dim, _layers, _heads, _maxT;
        private readonly EmbeddingLayer _tokEmb;
        private readonly PositionalEncoding _pos;
        private readonly TransformerBlock[] _blocks;

        // Make Wout public so we can train it
        public float[] Wout { get; } // [dim, vocab]

        public int Dim => _dim;
        public int VocabSize => _vocab;
        public int MaxSeq => _maxT;

        public TinyTransformer(int vocabSize, int dim = 64, int heads = 2, int layers = 2, int maxSeq = 64, int ffHidden = 4, int seed = 42)
        {
            _vocab = vocabSize; _dim = dim; _layers = layers; _heads = heads; _maxT = maxSeq;
            var rng = new Random(seed);
            _tokEmb = new EmbeddingLayer(vocabSize, dim, rng);
            _pos = new PositionalEncoding(maxSeq, dim);
            _blocks = Enumerable.Range(0, layers).Select(_ => new TransformerBlock(dim, heads, ffHidden * dim, rng)).ToArray();
            Wout = TinyMath.Rand(dim * vocabSize, 0.02f, rng);
        }

        /// Forward to logits [T, vocab] (flattened)
        public float[] ForwardLogits(int[] tokens)
        {
            var x = ForwardHidden(tokens);
            var logits = new float[tokens.Length * _vocab];
            TinyMath.MatMul(x, Wout, logits, tokens.Length, _dim, _vocab);
            return logits;
        }

        /// NEW: Forward to final hidden states [T, dim] (flattened). Used for training the head.
        public float[] ForwardHidden(int[] tokens)
        {
            if (tokens.Length > _maxT) throw new ArgumentException($"seq length {tokens.Length} > max {_maxT}");
            int T = tokens.Length; int D = _dim;

            var x = new float[T * D];
            _tokEmb.Forward(tokens, x);
            _pos.AddInPlace(x, T);
            foreach (var blk in _blocks) blk.Forward(x, T);
            return x; // [T*D]
        }
    }
}
