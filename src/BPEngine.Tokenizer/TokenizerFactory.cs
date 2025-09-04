using BPEngine.Tokenizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public static class TokenizerFactory
    {
        public static ByteLevelBPETokenizer CreateFromFiles(
           string mergesPath,
           string? vocabPath,
           IEnumerable<(string token, int id)> specials)
        {
            var ranks = MergesReader.LoadRanks(mergesPath);
            Dictionary<string, int>? vocab = null;
            if (!vocabPath.IsNullOrWhiteSpace())
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

        /// <summary>
        /// GPT-2 style: byte-level BPE from merges+vocab (your existing tokenizer).
        /// </summary>
        public static ITokenizer CreateGpt2(string mergesPath, Dictionary<string, int>? vocab, TokenizerOptions? opt = null)
        {
            opt ??= new();
            return new ByteLevelBPETokenizer(mergesPath, vocab, specialTokenToId:null, opt);
        }

        /// <summary>
        /// TikToken-style: mergeable ranks + optional specials.
        /// </summary>
        public static ITokenizer CreateCl100k(string ranksJsonPath, string? specialsJsonPath = null, TokenizerOptions? opt = null)
        {
            opt ??= new(RegexPreset.Cl100k);
            var model = TikTokenModel.LoadFromJson(ranksJsonPath, specialsJsonPath);
            return new TikTokenTokenizer(model, opt);
        }
    }
}
