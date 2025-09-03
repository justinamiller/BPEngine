using BPEngine.Transformers;

namespace BPEngine.Cli.Commands
{
    internal static class TransformerCommand
    {
        public static int Run(string[] args)
        {
            var (flags, pos) = ArgParser.Parse(args);
            if (pos.Count == 0 || pos[0] != "demo")
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  bpe transformer demo --merges merges.txt [--vocab vocab.json] [--prompt \"Hello\"]");
                Console.WriteLine("                       [--layers 2] [--heads 2] [--dim 64] [--max-seq 64]");
                Console.WriteLine("                       [--max-new 30] [--temp 1.0] [--topk 0]");
                return ExitCodes.BadArgs;
            }

            var merges = flags.Require("--merges");
            var vocab = flags.Optional("--vocab");
            var prompt = flags.Optional("--prompt") ?? "";
            int layers = int.TryParse(flags.Optional("--layers"), out var L) ? L : 2;
            int heads = int.TryParse(flags.Optional("--heads"), out var H) ? H : 2;
            int dim = int.TryParse(flags.Optional("--dim"), out var D) ? D : 64;
            int maxSeq = int.TryParse(flags.Optional("--max-seq"), out var MS) ? MS : 64;
            int maxNew = int.TryParse(flags.Optional("--max-new"), out var MN) ? MN : 30;
            float temp = float.TryParse(flags.Optional("--temp"), out var T) ? T : 1.0f;
            int topK = int.TryParse(flags.Optional("--topk"), out var K) ? K : 0;

            var specials = new Dictionary<string, int>(); // add if you use <|bos|>, etc.

            var text = DemoRunner.Run(
                mergesPath: merges, vocabPath: vocab, specials: specials,
                prompt: prompt, maxNewTokens: maxNew,
                dim: dim, heads: heads, layers: layers, maxSeq: maxSeq,
                temperature: temp, topK: topK
            );

            Console.WriteLine(text);
            return ExitCodes.Ok;
        }
    }
}
