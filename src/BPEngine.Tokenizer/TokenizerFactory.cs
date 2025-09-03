using BPEngine.Tokenizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    internal static class TokenizerFactory
    {
        public static ByteLevelBPETokenizer CreateFromFiles(
           string mergesPath,
           string? vocabPath,
           IEnumerable<(string token, int id)> specials)
        {
            var ranks = MergesReader.LoadRanks(mergesPath);
            Dictionary<string, int>? vocab = null;
            if (!string.IsNullOrWhiteSpace(vocabPath))
                vocab = VocabJsonReader.Load(vocabPath);
            var specialMap = specials?.ToDictionary(x => x.token, x => x.id) ?? new();
            return new ByteLevelBPETokenizer(mergesPath, vocab, specialMap);
        }

        public static ITokenizer CreateInstrumentedFromFiles(
      string mergesPath, string? vocabPath,
      IEnumerable<(string token, int id)> specials,
      Metrics.ITokenizerMetrics? metrics = null)
        {
            var vocab = string.IsNullOrWhiteSpace(vocabPath) ? null : VocabJsonReader.Load(vocabPath);
            var baseTok = new ByteLevelBPETokenizer(mergesPath, vocab, specials?.ToDictionary(x => x.token, x => x.id));
            return new Metrics.InstrumentedTokenizer(baseTok, metrics);
        }
    }
}
