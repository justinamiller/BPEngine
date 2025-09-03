using BPEngine.Transformers.Mathx;

namespace BPEngine.Transformers
{
    /// <summary>
    /// Mini GPT-like Transformer (CPU) for experimentation.
    /// Random weights (no training). Forward-only generation.
    /// </summary>
    public sealed class TinyTransformer
    {
        private readonly int _vocab, _dim, _layers, _heads, _maxT;
        private readonly EmbeddingLayer _tokEmb;
        private readonly PositionalEncoding _pos;
        private readonly TransformerBlock[] _blocks;
        private readonly float[] _Wout; // [D,vocab]

        public TinyTransformer(int vocabSize, int dim = 64, int heads = 2, int layers = 2, int maxSeq = 64, int ffHidden = 4)
        {
            _vocab = vocabSize; _dim = dim; _layers = layers; _heads = heads; _maxT = maxSeq;
            var rng = new Random(42);
            _tokEmb = new EmbeddingLayer(vocabSize, dim, rng);
            _pos = new PositionalEncoding(maxSeq, dim);
            _blocks = Enumerable.Range(0, layers).Select(_ => new TransformerBlock(dim, heads, ffHidden * dim, rng)).ToArray();
            _Wout = TinyMath.Rand(dim * vocabSize, 0.02f, rng);
        }

        /// <summary>Forward pass: tokens[T] -> logits[T, vocab].</summary>
        public float[] ForwardLogits(int[] tokens)
        {
            if (tokens.Length > _maxT) throw new ArgumentException($"seq length {tokens.Length} > max {_maxT}");
            int T = tokens.Length; int D = _dim;

            var x = new float[T * D];
            _tokEmb.Forward(tokens, x);
            _pos.AddInPlace(x, T);

            foreach (var blk in _blocks) blk.Forward(x, T);

            // logits = x @ Wout
            var logits = new float[T * _vocab];
            TinyMath.MatMul(x, _Wout, logits, T, D, _vocab);
            return logits;
        }
    }
}
