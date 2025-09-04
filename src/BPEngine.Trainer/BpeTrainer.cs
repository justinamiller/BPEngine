
using BPEngine.Tokenizer;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

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

        public async Task TrainAsync(
    string corpusPath,
    string mergesOutPath,
    string vocabOutPath,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(corpusPath))
                throw new ArgumentException("corpusPath is required", nameof(corpusPath));
            if (!File.Exists(corpusPath))
                throw new FileNotFoundException("Corpus file not found", corpusPath);

            await TrainAsync(new[] { corpusPath }, mergesOutPath, vocabOutPath, ct).ConfigureAwait(false);
        }

        public async Task TrainAsync(
            IEnumerable<string> corpusPaths,
            string mergesOutPath,
            string vocabOutPath,
            CancellationToken ct = default)
        {
            if (corpusPaths is null) throw new ArgumentNullException(nameof(corpusPaths));
            var paths = corpusPaths.ToArray();
            if (paths.Length == 0) throw new ArgumentException("At least one corpus path is required", nameof(corpusPaths));
            foreach (var p in paths)
                if (!File.Exists(p)) throw new FileNotFoundException("Corpus file not found", p);

            // Compute in-memory artifacts (reuses your existing training routine)
            ct.ThrowIfCancellationRequested();
            var (merges, vocab) = Train(paths);

            // Ensure output dir exists
            var outDir = Path.GetDirectoryName(Path.GetFullPath(mergesOutPath));
            if (!outDir.IsNullOrEmpty()) Directory.CreateDirectory(outDir);
            outDir = Path.GetDirectoryName(Path.GetFullPath(vocabOutPath));
            if (!outDir.IsNullOrEmpty()) Directory.CreateDirectory(outDir);

            // Write to temp files first (atomic publish)
            var tmpMerges = mergesOutPath + ".tmp";
            var tmpVocab = vocabOutPath + ".tmp";

            // 1) merges.txt — one pair per line: "left right"
            await using (var fs = new FileStream(tmpMerges, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            await using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                // (Optional) GPT-2 files sometimes start with a version comment; safe to omit.
                // await sw.WriteLineAsync("#version: bpe").ConfigureAwait(false);

                foreach (var (Left, Right) in merges)
                {
                    ct.ThrowIfCancellationRequested();
                    await sw.WriteLineAsync($"{Left} {Right}").ConfigureAwait(false);
                }
            }

            // 2) vocab.json — token -> id
            var jsonOpts = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // keep full unicode, avoid over-escaping
            };

            await using (var fs = new FileStream(tmpVocab, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(fs, vocab, jsonOpts, ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();

            // Atomic publish (replace if exists)
            if (File.Exists(mergesOutPath)) File.Delete(mergesOutPath);
            if (File.Exists(vocabOutPath)) File.Delete(vocabOutPath);
            File.Move(tmpMerges, mergesOutPath);
            File.Move(tmpVocab, vocabOutPath);
        }

        /// <summary>
        /// Synchronous helper: train and write artifacts to files (merges.txt + vocab.json).
        /// </summary>
        public void TrainToFiles(string corpusPath, string mergesOutPath, string vocabOutPath)
            => TrainToFiles(new[] { corpusPath }, mergesOutPath, vocabOutPath);

        /// <summary>
        /// Synchronous helper: train and write artifacts to files (merges.txt + vocab.json).
        /// </summary>
        public void TrainToFiles(IEnumerable<string> corpusPaths, string mergesOutPath, string vocabOutPath)
        {
            if (corpusPaths is null) throw new ArgumentNullException(nameof(corpusPaths));
            var paths = corpusPaths.ToArray();
            if (paths.Length == 0) throw new ArgumentException("At least one corpus path is required", nameof(corpusPaths));
            foreach (var p in paths)
                if (!File.Exists(p)) throw new FileNotFoundException("Corpus file not found", p);

            var (merges, vocab) = Train(paths);

            var outDir = Path.GetDirectoryName(Path.GetFullPath(mergesOutPath));
            if (!outDir.IsNullOrEmpty()) Directory.CreateDirectory(outDir);
            outDir = Path.GetDirectoryName(Path.GetFullPath(vocabOutPath));
            if (!outDir.IsNullOrEmpty()) Directory.CreateDirectory(outDir);

            var tmpMerges = mergesOutPath + ".tmp";
            var tmpVocab = vocabOutPath + ".tmp";

            using (var fs = new FileStream(tmpMerges, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                foreach (var (Left, Right) in merges)
                    sw.WriteLine($"{Left} {Right}");
            }

            var jsonOpts = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            using (var fs = new FileStream(tmpVocab, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(fs, vocab, jsonOpts);
            }

            if (File.Exists(mergesOutPath)) File.Delete(mergesOutPath);
            if (File.Exists(vocabOutPath)) File.Delete(vocabOutPath);
            File.Move(tmpMerges, mergesOutPath);
            File.Move(tmpVocab, vocabOutPath);
        }

    }
}
