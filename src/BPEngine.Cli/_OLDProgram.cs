using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;
using BPEngine.Trainer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BPEngine.Cli
{
    internal static class _OLDProgram
    {
        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                PrintHelp();
                return 0;
            }

            try
            {
                return args[0] switch
                {
                    "encode" => Encode(args),
                    "decode" => Decode(args),
                    "train" => Train(args),
                    _ => Unknown(args[0])
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
#if DEBUG
                Console.Error.WriteLine(ex);
#endif
                return 2;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("BPEngine CLI");
            Console.WriteLine();
            Console.WriteLine("Commands");
            Console.WriteLine("  encode   Tokenize text into IDs");
            Console.WriteLine("  decode   Convert IDs back to text");
            Console.WriteLine("  train    Learn merges + vocab from a text corpus");
            Console.WriteLine();
            Console.WriteLine("Usage");
            Console.WriteLine("  bpe encode --merges <path> [--vocab <path>] [--bos <tok>:<id>] [--eos <tok>:<id>] --text \"...\"");
            Console.WriteLine("  bpe decode --merges <path> --vocab <path> [--bos <tok>:<id>] [--eos <tok>:<id>] --ids 0,1,2");
            Console.WriteLine("  bpe train  --corpus <file1>[,<file2>...] --vocab-size 5000 [--min-pair 2] [--out ./artifacts] [--bos <tok>:<id>] [--eos <tok>:<id>] [--pad <tok>:<id>]");
            Console.WriteLine();
        }

        static int Unknown(string cmd)
        {
            Console.Error.WriteLine($"Unknown command: {cmd}");
            PrintHelp();
            return 1;
        }

        // ---------------- encode ----------------
        static int Encode(string[] args)
        {
            string? merges = null, vocabPath = null, text = null;
            var specials = new List<(string Token, int Id)>();
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--merges": merges = RequireValue(args, ref i); break;
                    case "--vocab": vocabPath = RequireValue(args, ref i); break;
                    case "--text": text = RequireValue(args, ref i); break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        specials.Add(ParseSpecial(RequireValue(args, ref i)));
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(merges) || string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("encode requires --merges and --text");

            Dictionary<string, int>? vocab = null;
            if (!string.IsNullOrWhiteSpace(vocabPath))
                vocab = VocabJsonReader.Load(vocabPath);

            var specialMap = specials.ToDictionary(t => t.Token, t => t.Id);
            var opts = new TokenizerOptions();
            var tok = new ByteLevelBPETokenizer(merges, vocab, specialMap, opts);

            var ids = tok.Encode(text);
            Console.WriteLine(string.Join(",", ids));
            return 0;
        }

        // ---------------- decode ----------------
        static int Decode(string[] args)
        {
            string? merges = null, vocabPath = null, idList = null;
            var specials = new List<(string Token, int Id)>();
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--merges": merges = RequireValue(args, ref i); break;
                    case "--vocab": vocabPath = RequireValue(args, ref i); break;
                    case "--ids": idList = RequireValue(args, ref i); break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        specials.Add(ParseSpecial(RequireValue(args, ref i)));
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(merges) || string.IsNullOrWhiteSpace(vocabPath) || string.IsNullOrWhiteSpace(idList))
                throw new ArgumentException("decode requires --merges, --vocab, and --ids");

            var vocab = VocabJsonReader.Load(vocabPath);
            var specialMap = specials.ToDictionary(t => t.Token, t => t.Id);
            var tok = new ByteLevelBPETokenizer(merges, vocab, specialMap);

            var ids = idList.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            Console.WriteLine(tok.Decode(ids));
            return 0;
        }

        // ---------------- train ----------------
        static int Train(string[] args)
        {
            var corpus = new List<string>();
            string? outDir = null;
            int vocabSize = 5000;
            int minPair = 2;
            var specials = new Dictionary<string, int>();

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--corpus":
                        corpus.AddRange(RequireValue(args, ref i).Split(',', StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "--out":
                        outDir = RequireValue(args, ref i);
                        break;
                    case "--vocab-size":
                        vocabSize = int.Parse(RequireValue(args, ref i));
                        break;
                    case "--min-pair":
                        minPair = int.Parse(RequireValue(args, ref i));
                        break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        {
                            var (tok, id) = ParseSpecial(RequireValue(args, ref i));
                            specials[tok] = id;
                            break;
                        }
                }
            }

            if (corpus.Count == 0)
                throw new ArgumentException("train requires --corpus <file1>[,<file2>...]");

            outDir ??= "./artifacts";
            Directory.CreateDirectory(outDir);

            var opts = new TrainerOptions
            {
                VocabSize = vocabSize,
                MinPairFrequency = minPair,
                Specials = specials
            };

            var trainer = new BpeTrainer(opts);
            var (merges, vocab) = trainer.Train(corpus);

            var mergesPath = Path.Combine(outDir, "merges.txt");
            var vocabPath = Path.Combine(outDir, "vocab.json");
            TrainerArtifacts.WriteMerges(mergesPath, merges);
            TrainerArtifacts.WriteVocab(vocabPath, vocab);

            Console.WriteLine($"Wrote {merges.Count} merges -> {mergesPath}");
            Console.WriteLine($"Wrote {vocab.Count} vocab entries -> {vocabPath}");
            return 0;
        }

        // ---------------- arg helpers ----------------
        static string RequireValue(string[] args, ref int i)
        {
            if (i + 1 >= args.Length) throw new ArgumentException($"Missing value after {args[i]}");
            return args[++i];
        }

        static (string Token, int Id) ParseSpecial(string spec)
        {
            var kv = spec.Split(':', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], out var id))
                throw new ArgumentException($"Invalid special token spec: {spec} (use <token>:<id>)");
            return (kv[0], id);
        }
    }
}
