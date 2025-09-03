using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BPEngine.Trainer;

namespace BPEngine.Cli.Commands
{
    internal static class TrainCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);

            var corpusCsv = flags.Require("--corpus", "train requires --corpus <file1>[,<file2>...]");
            var outDir = flags.Optional("--out") ?? "./artifacts";
            var vocabSize = int.TryParse(flags.Optional("--vocab-size"), out var vs) ? vs : 5000;
            var minPair = int.TryParse(flags.Optional("--min-pair"), out var mp) ? mp : 2;
            var specials = ParseSpecials(flags);

            var corpus = corpusCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
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
            return ExitCodes.Ok;
        }

        private static Dictionary<string, int> ParseSpecials(Dictionary<string, List<string>> flags)
        {
            var dict = new Dictionary<string, int>();
            foreach (var key in new[] { "--bos", "--eos", "--pad" })
            {
                foreach (var spec in flags.Multi(key))
                {
                    var kv = spec.Split(':', 2);
                    if (kv.Length != 2 || !int.TryParse(kv[1], out var id))
                        throw new ArgumentException($"Invalid special token spec for {key}: {spec} (use <token>:<id>)");
                    dict[kv[0]] = id;
                }
            }
            return dict;
        }
    }
}
