using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace BPEngine.Models
{
    public static class NGramSampler
    {
        /// <summary>
        /// Generates text using a trained model. If a prompt is given,
        /// it tokenizes it and uses it as history.
        /// </summary>
        public static string Generate(
            NGramModel model,
            string mergesPath,
            string? vocabPath,
            Dictionary<string, int>? specials,
            string prompt,
            int maxTokens = 50,
            float temperature = 1.0f,
            int topK = 0)
        {
            var vocab = string.IsNullOrWhiteSpace(vocabPath) ? null : VocabJsonReader.Load(vocabPath);
            var tok = new ByteLevelBPETokenizer(mergesPath, vocab, specials ?? new());

            var history = new List<int>();
            if (!string.IsNullOrEmpty(prompt))
                history.AddRange(tok.Encode(prompt));

            var rng = new Random();
            var gen = new List<int>();

            for (int i = 0; i < maxTokens; i++)
            {
                int next = model.SampleNext(CollectionsMarshal.AsSpan(history), rng, temperature, topK);
                gen.Add(next);
                history.Add(next);
            }

            return tok.Decode(gen);
        }
    }
}
