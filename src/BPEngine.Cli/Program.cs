using BPEngine.Cli.Commands;
using System;

namespace BPEngine.Cli
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                PrintHelp();
                return ExitCodes.Ok;
            }

            var (cmd, rest) = ArgParser.SplitCommand(args);

            try
            {
                return cmd switch
                {
                    "encode" => Commands.EncodeCommand.Run(rest),
                    "decode" => Commands.DecodeCommand.Run(rest),
                    "train" => Commands.TrainCommand.Run(rest),
                    "validate" => Commands.ValidateCommand.Run(rest),
                    "ngram" => Commands.NGramCommand.Run(rest),
                    "analyze" => Commands.AnalyzeCommand.Run(rest),
                    "transformer" => Commands.TransformerCommand.Run(rest),
                    "snippet" => Commands.SnippetCommand.Run(rest),
                    "train-head" => Commands.TransformerHeadCommands.TrainHead(rest),
                    "gen-head" => Commands.TransformerHeadCommands.GenerateWithHead(rest),
                    "rag" => RagCommand.Run(rest),
                    _ => Unknown(cmd)
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
#if DEBUG
                Console.Error.WriteLine(ex);
#endif
                return ExitCodes.Error;
            }
        }
        static void PrintHelp()
        {
            Console.WriteLine("BPEngine CLI");
            Console.WriteLine();
            Console.WriteLine("Commands (input → output):");
            Console.WriteLine("  encode        text → token IDs");
            Console.WriteLine("                (input: --text \"...\"; output: comma-separated IDs)");
            Console.WriteLine();
            Console.WriteLine("  decode        token IDs → text");
            Console.WriteLine("                (input: --ids 1,2,3; output: reconstructed string)");
            Console.WriteLine();
            Console.WriteLine("  train         text corpus → merges + vocab");
            Console.WriteLine("                (input: --corpus file.txt; output: merges.txt + vocab.json)");
            Console.WriteLine();
            Console.WriteLine("  validate      merges/vocab → sanity check round-trip");
            Console.WriteLine("                (input: merges.txt + vocab.json; output: reports on consistency)");
            Console.WriteLine();
            Console.WriteLine("  analyze       text corpus → token statistics");
            Console.WriteLine("                (input: --corpus file.txt; output: histogram, top tokens, bigrams)");
            Console.WriteLine();
            Console.WriteLine("  snippet       long text + budget → shortened text");
            Console.WriteLine("                (input: --text \"...\" --budget N; output: trimmed snippet + tokens used)");
            Console.WriteLine();
            Console.WriteLine("  ngram train   text corpus → n-gram model JSON");
            Console.WriteLine("                (input: --corpus file.txt; output: ngram.json)");
            Console.WriteLine("  ngram generate model + prompt → generated text");
            Console.WriteLine("                (input: --model ngram.json --prompt \"...\"; output: sampled text)");
            Console.WriteLine();
            Console.WriteLine("  transformer demo prompt → toy generated text (random weights)");
            Console.WriteLine("                (input: --prompt \"...\"; output: text showing transformer mechanics)");
            Console.WriteLine();
            Console.WriteLine("  train-head    text corpus → trained head weights");
            Console.WriteLine("                (input: --corpus file.txt; output: wout.bin weight file)");
            Console.WriteLine("  gen-head      prompt + trained head → generated text (learned patterns)");
            Console.WriteLine("                (input: --prompt \"...\" --wout wout.bin; output: generated text)");
            Console.WriteLine("  rag query     query + corpus → top-K matches");
            Console.WriteLine("                (input: --q \"...\" --corpus file.txt; output: doc scores)");
            Console.WriteLine();
            Console.WriteLine("Global flags (accepted by most commands):");
            Console.WriteLine("  --perf          Print perf metrics (human-readable)");
            Console.WriteLine("  --perf-json     Print perf metrics as JSON");
            Console.WriteLine("  --json / --csv  Some outputs honor ConsoleFormats style if your command uses it");
            Console.WriteLine();
            Console.WriteLine("Quick examples:");
            Console.WriteLine("  bpe encode   --merges merges.txt --vocab vocab.json --text \"Hello world\"");
            Console.WriteLine("  bpe decode   --merges merges.txt --vocab vocab.json --ids 15496,995");
            Console.WriteLine("  bpe train    --corpus data.txt --vocab-size 5000 --min-pair 2 --out ./artifacts");
            Console.WriteLine("  bpe analyze  --corpus data.txt --merges merges.txt --vocab vocab.json");
            Console.WriteLine("  bpe snippet  --merges merges.txt --vocab vocab.json --text \"Long input...\" --budget 256");
            Console.WriteLine("  bpe ngram train    --order 3 --corpus data.txt --merges merges.txt --vocab vocab.json --out ./artifacts/ngram.json");
            Console.WriteLine("  bpe ngram generate --order 3 --model ./artifacts/ngram.json --merges merges.txt --vocab vocab.json --prompt \"Hello\" --max 50");
            Console.WriteLine("  bpe transformer demo --merges merges.txt --vocab vocab.json --prompt \"Hello\" --layers 2 --heads 2 --dim 64 --max-seq 64 --max-new 30");
            Console.WriteLine("  bpe train-head --merges merges.txt --vocab vocab.json --corpus data.txt --out ./artifacts/wout.bin");
            Console.WriteLine("  bpe gen-head   --merges merges.txt --vocab vocab.json --wout ./artifacts/wout.bin --prompt \"Test prompt\"");
            Console.WriteLine("  bpe rag build  --corpus data.txt --out ./artifacts/tfidf.idx");
            Console.WriteLine("  bpe rag query  --corpus data.txt --q \"login error\"");
            Console.WriteLine();
            Console.WriteLine("Details: use a command with --help for more options, e.g.:");
            Console.WriteLine("  bpe ngram --help");
            Console.WriteLine("  bpe transformer --help");
            Console.WriteLine("  bpe rag --help");
            Console.WriteLine();
        }


        static int Unknown(string cmd)
        {
            Console.Error.WriteLine($"Unknown command: {cmd}");
            Console.Error.WriteLine("Use --help for usage.");
            return ExitCodes.BadArgs;
        }
    }
}
