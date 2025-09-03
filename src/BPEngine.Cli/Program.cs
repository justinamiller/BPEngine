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
            Console.WriteLine("Commands");
            Console.WriteLine("  encode     Tokenize text into IDs");
            Console.WriteLine("  decode     Convert IDs back to text");
            Console.WriteLine("  train      Learn merges + vocab from a text corpus");
            Console.WriteLine("  validate   Sanity checks for merges/vocab + round-trip");
            Console.WriteLine();
            Console.WriteLine("Usage (examples)");
            Console.WriteLine("  bpe encode   --merges merges.txt --vocab vocab.json --bos <|bos|>:0 --eos <|eos|>:1 --text \"...\"");
            Console.WriteLine("  bpe decode   --merges merges.txt --vocab vocab.json --ids 0,1,2");
            Console.WriteLine("  bpe train    --corpus data.txt --vocab-size 5000 --min-pair 2 --out ./artifacts --bos <|bos|>:0 --eos <|eos|>:1");
            Console.WriteLine("  bpe validate --merges merges.txt --vocab vocab.json --sample \"Hello, world!\"");
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
