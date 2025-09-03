using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;

namespace BPEngine.Models
{
    public static class NGramTrainer
    {
        /// <summary>
        /// Trains an n-gram model from a text corpus using your tokenizer.
        /// </summary>
        public static NGramModel TrainFromFiles(
            int order,
            string mergesPath,
            string? vocabPath,
            Dictionary<string, int>? specials,
            IEnumerable<string> corpusPaths,
            int maxLines = int.MaxValue)
        {
            var vocab = vocabPath.IsNullOrWhiteSpace() ? null : VocabJsonReader.Load(vocabPath);
            var tok = new ByteLevelBPETokenizer(mergesPath, vocab, specials ?? new());
            int vocabSize = (vocab?.Count ?? 30000) + (specials?.Count ?? 0); // rough cap if no vocab
            var model = new NGramModel(order: order, addK: 1.0f, vocabSize: vocabSize);

            foreach (var path in corpusPaths)
            {
                int lines = 0;
                foreach (var line in File.ReadLines(path))
                {
                    var ids = tok.Encode(line);
                    if (ids.Length == 0) continue;
                    model.Observe(ids);
                    if (++lines >= maxLines) break;
                }
            }
            return model;
        }
    }
}
