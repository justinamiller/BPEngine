using BPEngine.Models;

namespace BPEngine.Cli.Commands
{
    internal static class NGramCommand
    {
        public static int Run(string[] args)
        {
            var (flags, pos) = ArgParser.Parse(args);
            if (pos.Count == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  bpe ngram train --order 3 --corpus file.txt --merges merges.txt [--vocab vocab.json] [--out model.json]");
                Console.WriteLine("  bpe ngram generate --order 3 --model model.json --merges merges.txt [--vocab vocab.json] [--prompt \"...\"] [--max 50] [--temp 1.0] [--topk 0]");
                return ExitCodes.BadArgs;
            }

            switch (pos[0])
            {
                case "train":
                    return Train(flags);
                case "generate":
                    return Generate(flags);
                default:
                    Console.Error.WriteLine($"Unknown ngram subcommand: {pos[0]}");
                    return ExitCodes.BadArgs;
            }
        }

        private static int Train(Dictionary<string, List<string>> flags)
        {
            int order = int.TryParse(flags.Optional("--order"), out var o) ? o : 3;
            var merges = flags.Require("--merges");
            var vocab = flags.Optional("--vocab");
            var outPath = flags.Optional("--out") ?? "./artifacts/ngram.json";
            var corpusCsv = flags.Require("--corpus");
            var corpus = corpusCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var specials = ParseSpecials(flags);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            var model = NGramTrainer.TrainFromFiles(order, merges, vocab, specials, corpus);
            model.Save(outPath);

            ConsoleFormats.ShowTrainSummary(
                outDir: Path.GetDirectoryName(outPath)!,
                mergesPath: merges,
                vocabPath: vocab ?? "(none)",
                mergesCount: 0, // not relevant for n-gram
                vocabCount: model.VocabSize,
                corpusFiles: corpus
            );

            ConsoleFormats.Info($"Saved n-gram (order={order}) -> {outPath}");
            return ExitCodes.Ok;
        }

        private static int Generate(Dictionary<string, List<string>> flags)
        {
            int order = int.TryParse(flags.Optional("--order"), out var o) ? o : 3;
            var merges = flags.Require("--merges");
            var vocab = flags.Optional("--vocab");
            var modelPath = flags.Require("--model");
            var prompt = flags.Optional("--prompt") ?? "";
            int max = int.TryParse(flags.Optional("--max"), out var m) ? m : 50;
            float temp = float.TryParse(flags.Optional("--temp"), out var t) ? t : 1.0f;
            int topK = int.TryParse(flags.Optional("--topk"), out var k) ? k : 0;

            var model = NGramModel.Load(modelPath);
            if (model.Order != order)
                ConsoleFormats.Warn($"Model order={model.Order} differs from --order={order}; using model value.");

            var text = NGramSampler.Generate(model, merges, vocab, ParseSpecials(flags), prompt, max, temp, topK);
            Console.WriteLine(text);
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
