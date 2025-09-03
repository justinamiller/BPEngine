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
            Console.WriteLine("  encode     Tokenize text into IDs");
            Console.WriteLine("  decode     Convert IDs back to text");
            Console.WriteLine("  train      Learn merges + vocab from a text corpus");
            Console.WriteLine("  validate   Sanity check merges/vocab + round-trip");
            Console.WriteLine("  ngram      Train or generate with a simple n-gram model");
            Console.WriteLine("  analyze    Compute token stats over a corpus");
            Console.WriteLine();
            Console.WriteLine("Usage Examples:");
            Console.WriteLine("  bpe encode   --merges merges.txt --vocab vocab.json --bos <|bos|>:0 --eos <|eos|>:1 --text \"Hello world\"");
            Console.WriteLine("  bpe decode   --merges merges.txt --vocab vocab.json --ids 15496,995");
            Console.WriteLine("  bpe train    --corpus data.txt --vocab-size 5000 --min-pair 2 --out ./artifacts --bos <|bos|>:0 --eos <|eos|>:1");
            Console.WriteLine("  bpe validate --merges merges.txt --vocab vocab.json --sample \"Hello world\"");
            Console.WriteLine();
            Console.WriteLine("  bpe ngram train    --order 3 --corpus data.txt --merges merges.txt --vocab vocab.json --out ./artifacts/ngram.json");
            Console.WriteLine("  bpe ngram generate --order 3 --model ./artifacts/ngram.json --merges merges.txt --vocab vocab.json --prompt \"Hello\" --max 50 --temp 1.0 --topk 0");
            Console.WriteLine();
            Console.WriteLine("  bpe analyze --corpus data.txt --merges merges.txt --vocab vocab.json --top 20 --bins 10");
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
