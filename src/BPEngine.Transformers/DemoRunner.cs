using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;

namespace BPEngine.Transformers
{
    public static class DemoRunner
    {
        public static string Run(
            string mergesPath, string? vocabPath,
            Dictionary<string, int>? specials,
            string prompt, int maxNewTokens = 30,
            int dim = 64, int heads = 2, int layers = 2, int maxSeq = 64,
            float temperature = 1.0f, int topK = 0)
        {
            var vocab = string.IsNullOrWhiteSpace(vocabPath) ? null : VocabJsonReader.Load(vocabPath);
            int vocabSize = (vocab?.Count ?? 30000);
            var tok = new ByteLevelBPETokenizer(mergesPath, vocab, specials ?? new());

            var model = new TinyTransformer(vocabSize, dim, heads, layers, maxSeq);

            var ids = prompt.Length == 0 ? Array.Empty<int>() : tok.Encode(prompt);
            var buf = new List<int>(ids);

            for (int step = 0; step < maxNewTokens; step++)
            {
                // crop to context window
                var context = buf.Count > maxSeq ? buf.Skip(buf.Count - maxSeq).ToArray() : buf.ToArray();
                var logits = model.ForwardLogits(context);
                int next = Sampling.SampleLast(logits, vocabSize, temperature, topK);
                buf.Add(next);
            }

            return tok.Decode(buf);
        }
    }
}
