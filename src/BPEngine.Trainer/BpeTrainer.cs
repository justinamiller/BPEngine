
using System.Text;
using BPEngine.Tokenizer;

namespace BPEngine.Trainer
{
    /// <summary>
    /// Minimal byte-level BPE trainer (demo) that learns merges from a corpus.
    /// Steps:
    /// 1) Pretokenize lines into tokens (GPT regex or whitespace).
    /// 2) Map tokens into byte-level unicode space.
    /// 3) Iteratively count pair frequencies and merge the most frequent pair.
    /// 4) Stop at MaxMerges or VocabSize - specials - 256 base symbols (roughly).
    /// 5) Build a simple vocab by applying merges to corpus and collecting unique pieces.
    /// </summary>
    public sealed class BpeTrainer
    {
        private readonly TrainerOptions _opts;
        private readonly IProgressReporter _progress;

        public BpeTrainer(TrainerOptions opts, IProgressReporter? progress = null)
        {
            _opts = opts;
            _progress = progress ?? new ConsoleProgressReporter();
        }

        public (List<(string Left, string Right)> merges, Dictionary<string,int> vocab) Train(IEnumerable<string> corpusPaths)
        {
            // 1) Stream tokens
            var rawTokens = CorpusReader.StreamTokens(corpusPaths, _opts.UseGptRegexPretokenizer);

            // 2) Byte-level unicode mapping
            var mapper = BPEngine.Tokenizer.Performance.ByteUnicodeMap.Build(); // array-backed for speed
            IEnumerable<string> MapToken(string t)
            {
                var bytes = Encoding.UTF8.GetBytes(t);
                var chars = new char[bytes.Length];
                for (int i = 0; i < bytes.Length; i++) chars[i] = mapper.ByteToChar[bytes[i]];
                yield return new string(chars);
            }
            var mappedTokens = rawTokens.SelectMany(MapToken).ToList();

            // 3) Iterative merges
            var merges = new List<(string Left, string Right)>();
            var tokenPieces = mappedTokens.Select(t => t.Select(ch => ch.ToString()).ToList()).ToList();

            int initialSymbols = 256; // byte-level base alphabet
            int targetMerges = _opts.MaxMerges ?? Math.Max(0, _opts.VocabSize - _opts.Specials.Count - initialSymbols);
            _progress.OnStart(initialSymbols, targetMerges);

            for (int step = 0; step < targetMerges; step++)
            {
                // Count pairs
                var pairFreq = new Dictionary<(string,string), int>(capacity: 1<<16);
                foreach (var pieces in tokenPieces)
                {
                    for (int i = 0; i < pieces.Count - 1; i++)
                    {
                        var key = (pieces[i], pieces[i+1]);
                        pairFreq.TryGetValue(key, out var c);
                        pairFreq[key] = c + 1;
                    }
                }
                if (pairFreq.Count == 0) break;

                // pick best
                var best = pairFreq
                    .Where(kv => kv.Value >= _opts.MinPairFrequency)
                    .OrderByDescending(kv => kv.Value)
                    .FirstOrDefault();

                if (best.Key == default) break; // no pair meets min freq

                var (left, right) = best.Key;
                var merged = left + right;
                merges.Add((left, right));
                _progress.OnMerge(step + 1, left, right, best.Value);

                // replace occurrences in all tokens
                foreach (var pieces in tokenPieces)
                {
                    if (pieces.Count < 2) continue;

                    var i = 0;
                    while (i < pieces.Count - 1)
                    {
                        if (pieces[i] == left && pieces[i+1] == right)
                        {
                            pieces[i] = merged;
                            pieces.RemoveAt(i+1);
                            // stay at same i to catch cascading merges
                        }
                        else i++;
                    }
                }
            }

            _progress.OnDone(merges.Count);

            // 4) Build a simple vocab by applying merges (already applied) and collecting unique pieces
            var vocab = new Dictionary<string,int>();
            // reserve specials
            foreach (var kv in _opts.Specials) vocab[kv.Key] = kv.Value;

            int nextId = vocab.Count;
            // include base byte-level symbols in vocab (single chars)
            for (int b = 0; b < 256; b++)
            {
                var ch = mapper.ByteToChar[b];
                var s = ch.ToString();
                if (!vocab.ContainsKey(s)) vocab[s] = nextId++;
            }

            // include merged pieces observed in corpus
            foreach (var pieces in tokenPieces)
            {
                foreach (var p in pieces)
                {
                    if (!vocab.ContainsKey(p)) vocab[p] = nextId++;
                }
            }

            return (merges, vocab);
        }
    }
}
