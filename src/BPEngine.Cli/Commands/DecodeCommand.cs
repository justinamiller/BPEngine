using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BPEngine.Cli.Commands
{
    internal static class DecodeCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);

            var merges = flags.Require("--merges");
            var vocab = flags.Require("--vocab");
            var idList = flags.Require("--ids");
            var specials = ParseSpecials(flags);

            var vocabMap = VocabJsonReader.Load(vocab);
            var tok = new ByteLevelBPETokenizer(
                mergesPath: merges,
                tokenToId: vocabMap,
                specialTokenToId: specials
            );

            var ids = idList.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            Console.WriteLine(tok.Decode(ids));
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
