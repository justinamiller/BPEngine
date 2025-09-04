using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BPEngine.Cli
{
    internal static class ConsoleFormats
    {
        // ---------- Simple setup ----------
        public enum Style { Plain, Pretty, Json, Csv }
        private static Style _style = Style.Pretty;
        public static void Setup(Style style = Style.Pretty) => _style = style;

        // ---------- One-liner facades (call these from commands) ----------
        public static void ShowEncodeResult(string mergesPath, string? vocabPath, string input, int[] ids)
        {
            switch (_style)
            {
                case Style.Json:
                    PrintJson(new { action = "encode", mergesPath, vocabPath, input, ids });
                    return;
                case Style.Csv:
                    Console.WriteLine(string.Join(",", ids));
                    return;
                case Style.Plain:
                    WriteLine($"ENCODE  merges={mergesPath} vocab={vocabPath ?? "-"}");
                    WriteLine($"INPUT   {input}");
                    WriteLine($"IDS     {string.Join(" ", ids)}");
                    return;
                default: // Pretty
                    Header("Encode");
                    KeyValues(new[] {
                        ("Merges", mergesPath),
                        ("Vocab",  vocabPath ?? "(none)"),
                        ("Input",  input)
                    });
                    Divider();
                    Label("IDs"); PrintIdGroups(ids);
                    return;
            }
        }

        /// <summary>
        /// Print a sequence of integers as a single comma-separated line.
        /// Example: 15496,995,123
        /// </summary>
        public static void PrintCsv(IEnumerable<int> values)
        {
            if (values is null)
            {
                Console.WriteLine(string.Empty);
                return;
            }

            Console.WriteLine(string.Join(",", values));
        }

        public static void ShowDecodeResult(string mergesPath, string vocabPath, int[] ids, string output)
        {
            switch (_style)
            {
                case Style.Json:
                    PrintJson(new { action = "decode", mergesPath, vocabPath, ids, output });
                    return;
                case Style.Csv:
                    Console.WriteLine(output);
                    return;
                case Style.Plain:
                    WriteLine($"DECODE  merges={mergesPath} vocab={vocabPath}");
                    WriteLine($"IDS     {string.Join(" ", ids)}");
                    WriteLine($"OUTPUT  {output}");
                    return;
                default:
                    Header("Decode");
                    KeyValues(new[] {
                        ("Merges", mergesPath),
                        ("Vocab",  vocabPath),
                        ("IDs",    string.Join(",", ids))
                    });
                    Divider();
                    Label("Output"); WriteLine(output);
                    return;
            }
        }

        public static void ShowTrainSummary(
            string outDir, string mergesPath, string vocabPath,
            int mergesCount, int vocabCount, IEnumerable<string> corpusFiles)
        {
            switch (_style)
            {
                case Style.Json:
                    PrintJson(new { action = "train", outDir, mergesPath, vocabPath, mergesCount, vocabCount, corpusFiles });
                    return;
                case Style.Plain:
                    WriteLine($"TRAIN   out={outDir}");
                    WriteLine($"MERGES  {mergesPath} ({mergesCount})");
                    WriteLine($"VOCAB   {vocabPath}  ({vocabCount})");
                    WriteLine("CORPUS  " + string.Join(", ", corpusFiles));
                    return;
                default:
                    Header("Train");
                    KeyValues(new[] {
                        ("Output Dir", outDir),
                        ("Merges",     $"{mergesPath} ({mergesCount})"),
                        ("Vocab",      $"{vocabPath} ({vocabCount})")
                    });
                    Divider();
                    Label("Corpus Files");
                    PrintColumns(corpusFiles, columns: 2);
                    return;
            }
        }

        public static void ShowValidate(string mergesPath, string vocabPath, string sample, int[] ids, string roundTrip)
        {
            var ok = roundTrip.Length > 0;
            switch (_style)
            {
                case Style.Json:
                    PrintJson(new { action = "validate", mergesPath, vocabPath, sample, ids, roundTrip, ok });
                    return;
                case Style.Plain:
                    WriteLine($"VALIDATE merges={mergesPath} vocab={vocabPath}");
                    WriteLine($"SAMPLE   {sample}");
                    WriteLine($"IDS      {string.Join(" ", ids)}");
                    WriteLine($"ROUNDTRIP {roundTrip}");
                    WriteLine(ok ? "OK" : "FAIL");
                    return;
                default:
                    Header("Validate");
                    KeyValues(new[] {
                        ("Merges", mergesPath),
                        ("Vocab",  vocabPath),
                        ("Sample", sample)
                    });
                    Divider();
                    Label("IDs"); PrintIdGroups(ids);
                    Divider();
                    Label("Round-trip"); WriteLine(roundTrip);
                    if (!ok) Error("Round-trip failed (empty output).");
                    return;
            }
        }

        // ---------- Tiny helpers your commands might also use ----------
        public static void Info(string msg) => WithColor(ConsoleColor.Green, () => WriteLine(msg));
        public static void Warn(string msg) => WithColor(ConsoleColor.Yellow, () => WriteLine(msg));
        public static void Error(string msg) => WithColor(ConsoleColor.Red, () => Console.Error.WriteLine(msg));

        // ---------- Internals (pretty mode) ----------
        internal static void Header(string title)
        {
            WithColor(ConsoleColor.Cyan, () => WriteLine(title.ToUpperInvariant()));
            Divider();
        }

        internal static void Divider(char ch = '─')
        {
            var w = SafeWidth();
            WithColor(ConsoleColor.DarkGray, () => WriteLine(new string(ch, Math.Max(20, w))));
        }

        internal static void Label(string text)
            => WithColor(ConsoleColor.DarkCyan, () => WriteLine(text));

        internal static void KeyValues(IEnumerable<(string Key, string Value)> pairs, int pad = 12)
        {
            foreach (var (k, v) in pairs)
            {
                WithColor(ConsoleColor.Gray, () => Console.Write(k.PadRight(pad)));
                WithColor(ConsoleColor.White, () => WriteLine(v));
            }
        }

        private static void PrintIdGroups(int[] ids, int group = 8)
        {
            if (ids.Length == 0) { WriteLine("(empty)"); return; }
            for (int i = 0; i < ids.Length; i += group)
            {
                var slice = ids.Skip(i).Take(group);
                WriteLine(string.Join(" ", slice.Select(x => x.ToString().PadLeft(5))));
            }
        }

        /// <summary>
        /// Print a formatted header line for CLI output.
        /// Example:
        /// ==== ENCODE RESULTS ====
        /// </summary>
        public static void PrintHeader(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine();
                return;
            }

            var line = new string('=', title.Length + 8); // pad with "==== " and " ===="
            Console.WriteLine(line);
            Console.WriteLine($"==== {title.ToUpperInvariant()} ====");
            Console.WriteLine(line);
        }

        private static void PrintColumns(IEnumerable<string> items, int columns = 2, int spacing = 3)
        {
            var list = items.ToList();
            if (list.Count == 0) { WriteLine("(none)"); return; }

            int w = SafeWidth();
            int colWidth = Math.Max(12, (w - spacing * (columns - 1)) / columns);
            for (int i = 0; i < list.Count; i += columns)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (c > 0) Console.Write(new string(' ', spacing));
                    var idx = i + c;
                    var cell = idx < list.Count ? Truncate(list[idx], colWidth) : "";
                    Console.Write(cell.PadRight(colWidth));
                }
                Console.WriteLine();
            }
        }

        private static void PrintJson(object obj)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(obj, opts));
        }

        private static void WithColor(ConsoleColor color, Action act)
        {
            if (_style != Style.Pretty) { act(); return; }
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try { act(); } finally { Console.ForegroundColor = prev; }
        }

        private static void WriteLine(string s) => Console.WriteLine(s);
        private static int SafeWidth() { try { return Math.Max(40, Console.WindowWidth); } catch { return 100; } }
        private static string Truncate(string s, int max) => s.Length <= max ? s : (max <= 1 ? s[..max] : s[..(max - 1)] + "…");

        public static void ShowPerf(Perf.PerfResult r, bool json = false)
        {
            if (json)
            {
                PrintJson(new
                {
                    r.Command,
                    r.Mode,
                    start_utc = r.StartTimeUtc.ToString("o"),
                    elapsed_ms = r.ElapsedMs,
                    cpu_ms = r.CpuMs,
                    tokens = r.Tokens,
                    iterations = r.Iterations,
                    tokens_per_sec = r.TokensPerSec,
                    peak_working_set_mb = r.PeakWorkingSetMB,
                    allocated_bytes = r.AllocatedBytes,
                    gc = new { gen0 = r.GC0, gen1 = r.GC1, gen2 = r.GC2 },
                    env = new
                    {
                        r.Env.OSDescription,
                        r.Env.OSArchitecture,
                        r.Env.ProcessArchitecture,
                        r.Env.FrameworkDescription,
                        logical_cores = r.Env.ProcessorCount
                    },
                    extra = r.Extra
                });
                return;
            }

            Header("Performance");
            KeyValues(new[]
            {
        ("Command", $"{r.Command} ({r.Mode})"),
        ("Start (UTC)", r.StartTimeUtc.ToString("o")),
        ("Wall (ms)", r.ElapsedMs.ToString("0.0")),
        ("CPU (ms)", r.CpuMs.ToString("0.0")),
        ("Tokens", r.Tokens.ToString()),
        ("Throughput", $"{r.TokensPerSec:0.0} tok/s"),
        ("Peak WS", $"{r.PeakWorkingSetMB:0.0} MB"),
        ("Allocated", $"{r.AllocatedBytes:n0} B"),
        ("GC", $"G0:{r.GC0} G1:{r.GC1} G2:{r.GC2}"),
        ("Runtime", r.Env.FrameworkDescription),
        ("OS/Arch", $"{r.Env.OSDescription} / {r.Env.ProcessArchitecture}"),
        ("Cores", r.Env.ProcessorCount.ToString())
    });
            if (r.Extra.Count > 0)
            {
                Divider();
                Label("Extra");
                foreach (var kv in r.Extra) Console.WriteLine($"{kv.Key.PadRight(18)}{kv.Value}");
            }
        }

    }
}
