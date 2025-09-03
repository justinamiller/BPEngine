using BPEngine.Tokenizer;

namespace BPEngine.Cli.Commands
{
    internal static class SnippetCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);
            var merges = flags.Require("--merges");
            var vocab = flags.Optional("--vocab");
            int budget = int.TryParse(flags.Optional("--budget"), out var b) ? b : 256;
            var text = flags.Require("--text");

            Dictionary<string, int>? v = null;
            if (!string.IsNullOrWhiteSpace(vocab)) v = VocabJsonReader.Load(vocab);
            var tok = new ByteLevelBPETokenizer(merges, v, new());

            var (snippet, used) = TokenBudgetTools.TrimToBudget(tok, text, budget);
            Console.WriteLine(snippet);
            Console.Error.WriteLine($"[used={used}/{budget} tokens]");
            return ExitCodes.Ok;
        }
    }
}
