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
            Console.WriteLine("Commands:");
            Console.WriteLine("  encode       Tokenize text into IDs");
            Console.WriteLine("  decode       Convert IDs back to text");
            Console.WriteLine("  train        Learn merges + vocab from a text corpus");
            Console.WriteLine("  validate     Sanity check merges/vocab + round-trip");
            Console.WriteLine("  analyze      Compute token stats over a corpus");
            Console.WriteLine("  ngram        Train or generate with a simple n-gram model");
            Console.WriteLine("  transformer  Tiny Transformer playground (forward-only demo)");
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
            Console.WriteLine("  bpe ngram train    --order 3 --corpus data.txt --merges merges.txt --vocab vocab.json --out ./artifacts/ngram.json");
            Console.WriteLine("  bpe ngram generate --order 3 --model ./artifacts/ngram.json --merges merges.txt --vocab vocab.json --prompt \"Hello\" --max 50");
            Console.WriteLine("  bpe transformer demo --merges merges.txt --vocab vocab.json --prompt \"Hello\" --layers 2 --heads 2 --dim 64 --max-seq 64 --max-new 30");
            Console.WriteLine();
            Console.WriteLine("Details: use a command with --help for more options, e.g.:");
            Console.WriteLine("  bpe ngram --help");
            Console.WriteLine("  bpe transformer --help");
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
