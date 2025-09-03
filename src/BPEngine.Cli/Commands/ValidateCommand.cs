using BPEngine.Tokenizer;
using BPEngine.Tokenizer.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace BPEngine.Cli.Commands
{
    internal static class ValidateCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);

            var merges = flags.Require("--merges");
            var vocab = flags.Require("--vocab");
            var sample = flags.Optional("--sample") ?? "<|bos|>hello, world!<|eos|>";

            if (!File.Exists(merges)) { Console.Error.WriteLine($"merges not found: {merges}"); return ExitCodes.NotFound; }
            if (!File.Exists(vocab)) { Console.Error.WriteLine($"vocab not found:  {vocab}"); return ExitCodes.NotFound; }

            var specials = new Dictionary<string, int>();
            foreach (var key in new[] { "--bos", "--eos", "--pad" })
            {
                foreach (var spec in flags.Multi(key))
                {
                    var kv = spec.Split(':', 2);
                    if (kv.Length != 2 || !int.TryParse(kv[1], out var id))
                        throw new ArgumentException($"Invalid special token spec for {key}: {spec} (use <token>:<id>)");
                    specials[kv[0]] = id;
                }
            }

            var vocabMap = VocabJsonReader.Load(vocab);
            var tok = new ByteLevelBPETokenizer(merges, vocabMap, specials);

            var ids = tok.Encode(sample);
            var back = tok.Decode(ids);

            ConsoleFormats.PrintHeader("Round-trip");
            Console.WriteLine($"Input : {sample}");
            Console.WriteLine($"IDs   : {string.Join(",", ids)}");
            Console.WriteLine($"Back  : {back}");

            if (back.Length == 0)
            {
                Console.Error.WriteLine("Decode produced empty output — check vocab/specials.");
                return ExitCodes.Invalid;
            }

            return ExitCodes.Ok;
        }
    }
}
