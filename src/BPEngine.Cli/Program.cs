using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;
using BPEngine.Trainer;
using System;
using System.Collections.Generic;
using System.IO;

namespace BPEngine.Cli
{
    class Program
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
                return 2;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("BPEngine CLI");
            Console.WriteLine("Usage:");
            Console.WriteLine("  bpe encode --merges <path> [--vocab <path>] [--bos <tok>:<id>] [--eos <tok>:<id>] --text \"...\"");
            Console.WriteLine("  bpe decode --merges <path> --vocab <path> [--bos <tok>:<id>] [--eos <tok>:<id>] --ids 0,1,2");
            Console.WriteLine("  bpe train  --corpus <file1>[,<file2>...] --vocab-size 5000 [--min-pair 2] [--out ./artifacts] [--bos <tok>:<id>] [--eos <tok>:<id>] [--pad <tok>:<id>]");
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
            var specials = new List<(string, int)>();

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--merges": merges = args[++i]; break;
                    case "--vocab": vocabPath = args[++i]; break;
                    case "--text": text = args[++i]; break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        ParseSpecial(args[++i], specials);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(merges) || string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("encode requires --merges and --text");

            Dictionary<string, int>? vocab = null;
            if (!string.IsNullOrWhiteSpace(vocabPath))
                vocab = VocabJsonReader.Load(vocabPath);

            var opts = new TokenizerOptions();
            var tok = new ByteLevelBPETokenizer(merges, vocab, specials.ToDictionary(t => t.Item1, t => t.Item2), opts);
            var ids = tok.Encode(text);
            Console.WriteLine(string.Join(",", ids));
            return 0;
        }

        // ---------------- decode ----------------
        static int Decode(string[] args)
        {
            string? merges = null, vocabPath = null, idList = null;
            var specials = new List<(string, int)>();

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--merges": merges = args[++i]; break;
                    case "--vocab": vocabPath = args[++i]; break;
                    case "--ids": idList = args[++i]; break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        ParseSpecial(args[++i], specials);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(merges) || string.IsNullOrWhiteSpace(vocabPath) || string.IsNullOrWhiteSpace(idList))
                throw new ArgumentException("decode requires --merges, --vocab, and --ids");

            var vocab = VocabJsonReader.Load(vocabPath);
            var tok = new ByteLevelBPETokenizer(merges, vocab, specials.ToDictionary(t => t.Item1, t => t.Item2));
            var ids = Array.ConvertAll(idList.Split(','), int.Parse);
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
                        corpus.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "--out":
                        outDir = args[++i];
                        break;
                    case "--vocab-size":
                        vocabSize = int.Parse(args[++i]);
                        break;
                    case "--min-pair":
                        minPair = int.Parse(args[++i]);
                        break;
                    case "--bos":
                    case "--eos":
                    case "--pad":
                        var (tok, id) = ParseSpecialRet(args[++i]);
                        specials[tok] = id;
                        break;
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

        // ---------------- helpers ----------------
        static void ParseSpecial(string spec, List<(string, int)> list)
        {
            var kv = spec.Split(':', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], out var id))
                throw new ArgumentException($"Invalid special token spec: {spec} (use <token>:<id>)");
            list.Add((kv[0], id));
        }

        static (string, int) ParseSpecialRet(string spec)
        {
            var kv = spec.Split(':', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], out var id))
                throw new ArgumentException($"Invalid special token spec: {spec} (use <token>:<id>)");
            return (kv[0], id);
        }
    }
}
