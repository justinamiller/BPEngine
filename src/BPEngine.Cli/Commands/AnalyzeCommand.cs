using BPEngine.Tokenizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BPEngine.Cli.Commands
{
    internal static class AnalyzeCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);

            // Required / optional args
            var corpusCsv = flags.Require("--corpus", "analyze requires --corpus <file1>[,<file2>...]");
            var merges = flags.Require("--merges");
            var vocabPath = flags.Optional("--vocab");
            int topN = int.TryParse(flags.Optional("--top"), out var t) ? t : 20;
            int bins = int.TryParse(flags.Optional("--bins"), out var b) ? Math.Max(5, b) : 10;

            var corpus = corpusCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var f in corpus)
            {
                if (!File.Exists(f))
                {
                    ConsoleFormats.Error($"Corpus file not found: {f}");
                    return ExitCodes.NotFound;
                }
            }

            // Load tokenizer
            Dictionary<string, int>? vocab = null;
            if (!string.IsNullOrWhiteSpace(vocabPath))
            {
                vocab = VocabJsonReader.Load(vocabPath!);
            }
            var tok = new ByteLevelBPETokenizer(merges, vocab, specialTokenToId: new Dictionary<string, int>());

            // Accumulators
            long totalTexts = 0;
            long totalTokens = 0;
            int minLen = int.MaxValue;
            int maxLen = 0;

            // Histogram (dynamic bin edges chosen after first pass; for simplicity we keep lengths)
            var lengths = new List<int>(capacity: 1_000);
            // Top tokens / bigrams
            var tokenFreq = new Dictionary<int, long>();
            var bigramFreq = new Dictionary<(int, int), long>();

            foreach (var path in corpus)
            {
                foreach (var line in File.ReadLines(path))
                {
                    var ids = tok.Encode(line ?? string.Empty);
                    totalTexts++;
                    totalTokens += ids.Length;
                    if (ids.Length < minLen) minLen = ids.Length;
                    if (ids.Length > maxLen) maxLen = ids.Length;

                    lengths.Add(ids.Length);

                    // token freq
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var id = ids[i];
                        tokenFreq.TryGetValue(id, out var c);
                        tokenFreq[id] = c + 1;

                        if (i + 1 < ids.Length)
                        {
                            var pair = (ids[i], ids[i + 1]);
                            bigramFreq.TryGetValue(pair, out var bc);
                            bigramFreq[pair] = bc + 1;
                        }
                    }
                }
            }

            // Derived stats
            double avgLen = totalTexts > 0 ? (double)totalTokens / totalTexts : 0.0;

            // Build histogram
            var hist = BuildHistogram(lengths, bins);

            // Top tokens / bigrams
            var topTokens = tokenFreq
                .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Take(topN)
                .Select(kv => new
                {
                    Id = kv.Key,
                    Count = kv.Value,
                    // Try to decode single-id token piece (may be empty if unknown)
                    Piece = SafeDecode(tok, kv.Key)
                })
                .ToList();

            var topBigrams = bigramFreq
                .OrderByDescending(kv => kv.Value)
                .Take(topN)
                .Select(kv => new
                {
                    Left = kv.Key.Item1,
                    Right = kv.Key.Item2,
                    Count = kv.Value,
                    PieceL = SafeDecode(tok, kv.Key.Item1),
                    PieceR = SafeDecode(tok, kv.Key.Item2)
                })
                .ToList();

            // ---- Output (easy to read, one-liners from ConsoleFormats) ----
            ConsoleFormats.ShowTrainSummary(
                outDir: "(analysis only)",
                mergesPath: merges,
                vocabPath: vocabPath ?? "(none)",
                mergesCount: 0,
                vocabCount: (vocab?.Count ?? 0),
                corpusFiles: corpus
            );

            ConsoleFormats.Header("Token Lengths");
            ConsoleFormats.KeyValues(new[]
            {
                ("Texts", totalTexts.ToString()),
                ("Total Tokens", totalTokens.ToString()),
                ("Avg Len", avgLen.ToString("0.00")),
                ("Min Len", minLen == int.MaxValue ? "n/a" : minLen.ToString()),
                ("Max Len", maxLen.ToString())
            });
            ConsoleFormats.Divider();

            ConsoleFormats.Label("Histogram (per line token counts)");
            foreach (var b in hist)
            {
                Console.WriteLine($"{b.Label.PadRight(12)} {new string('█', (int)Math.Round(b.Fraction * 40))} {b.Count}");
            }

            ConsoleFormats.Divider();
            ConsoleFormats.Label($"Top {topN} Tokens");
            PrintSimpleTable(
                topTokens.Select(t => new[] {
                    t.Id.ToString(),
                    t.Count.ToString(),
                    t.Piece.Replace("\n","\\n").Replace("\r","\\r")
                }),
                headers: new[] { "ID", "Count", "Piece" },
                widths: new[] { 8, 10, 40 }
            );

            ConsoleFormats.Divider();
            ConsoleFormats.Label($"Top {topN} Bigrams");
            PrintSimpleTable(
                topBigrams.Select(t => new[] {
                    $"{t.Left},{t.Right}",
                    t.Count.ToString(),
                    $"{t.PieceL} ▷ {t.PieceR}".Replace("\n","\\n").Replace("\r","\\r")
                }),
                headers: new[] { "Pair(ID,ID)", "Count", "Pieces" },
                widths: new[] { 16, 10, 50 }
            );

            return ExitCodes.Ok;
        }

        // ---- helpers ----

        private static string SafeDecode(ByteLevelBPETokenizer tok, int id)
        {
            try { return tok.Decode(new[] { id }); }
            catch { return ""; }
        }

        private static List<(string Label, int Count, double Fraction)> BuildHistogram(List<int> lengths, int bins)
        {
            var result = new List<(string, int, double)>();
            if (lengths.Count == 0) return result;

            int min = lengths.Min();
            int max = lengths.Max();
            if (min == max) // all same length
            {
                result.Add(($"[{min}]", lengths.Count, 1.0));
                return result;
            }

            double width = (max - min + 1) / (double)bins;
            var counts = new int[bins];

            foreach (var L in lengths)
            {
                int idx = (int)Math.Floor((L - min) / width);
                if (idx >= bins) idx = bins - 1;
                if (idx < 0) idx = 0;
                counts[idx]++;
            }

            int total = lengths.Count;
            for (int i = 0; i < bins; i++)
            {
                int start = (int)Math.Round(min + i * width);
                int end = (int)Math.Round(min + (i + 1) * width) - 1;
                if (i == bins - 1) end = max;
                string label = start == end ? $"[{start}]" : $"[{start}-{end}]";
                double frac = total > 0 ? (double)counts[i] / total : 0.0;
                result.Add((label, counts[i], frac));
            }

            return result;
        }

        private static void PrintSimpleTable(IEnumerable<string[]> rows, string[] headers, int[] widths)
        {
            // header
            for (int i = 0; i < headers.Length; i++)
            {
                if (i > 0) Console.Write(" | ");
                Console.Write(headers[i].PadRight(widths[i]));
            }
            Console.WriteLine();
            ConsoleFormats.Divider();

            // rows
            foreach (var r in rows)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    if (i > 0) Console.Write(" | ");
                    var cell = i < r.Length ? r[i] : "";
                    if (cell.Length > widths[i]) cell = cell.Substring(0, Math.Max(0, widths[i] - 1)) + "…";
                    Console.Write(cell.PadRight(widths[i]));
                }
                Console.WriteLine();
            }
        }
    }
}
