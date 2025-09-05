using System;
using System.IO;
using System.Linq;
using BPEngine.Tokenizer;
using BPEngine.Transformers;

namespace BPEngine.Cli.Commands
{
    /// <summary>
    /// Trains ONLY the output head (Wout) on prediction of next token (SFT-lite).
    /// Input: a plain corpus file (already good enough for a demo).
    /// </summary>
    internal static class SftTrainCommand
    {
        public static int Run(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);

            var merges = flags.Optional("--merges");
            var vocab = flags.Optional("--vocab");
            var ranks = flags.Optional("--ranks"); // if you want cl100k path
            var corpus = flags.Require("--corpus");
            var outPath = flags.Optional("--out") ?? "./artifacts/wout.bin";

            int dim = flags.Int("--dim", 128);
            int heads = flags.Int("--heads", 4);
            int layers = flags.Int("--layers", 4);
            int seqlen = flags.Int("--seqlen", 256);
            int batch = flags.Int("--batch", 8);
            int steps = flags.Int("--steps", 500);
            float lr = flags.Float("--lr", 1e-2f);

            // Build tokenizer
            ITokenizer tok;
            if (!ranks.IsNullOrWhiteSpace())
            {
                tok = TokenizerFactory.CreateCl100k(ranks!); // assumes you have this factory
            }
            else
            {
                var vocabMap = !vocab.IsNullOrWhiteSpace() ? VocabJsonReader.Load(vocab!) : null;
                tok = TokenizerFactory.CreateGpt2(merges ?? throw new ArgumentException("need --merges for gpt2 preset"), vocabMap);
            }

            // Read corpus and tokenize once (for demo scale)
            var text = File.ReadAllText(corpus);
            var ids = tok.Encode(text);

            // Create tiny model + trainer
            var model = new TinyTransformer(vocabSize: EstimateVocab(tok), dim: dim, nHeads: heads, nLayers: layers, maxSeq: seqlen);
            var trainer = new HeadTrainer(model);
            var cfg = new HeadTrainer.TrainConfig(
                BatchSize: batch, SeqLen: seqlen, Steps: steps, LR: lr
            );

            Console.WriteLine($"[sft] tokens={ids.Length} dim={dim} heads={heads} layers={layers} seq={seqlen} batch={batch} steps={steps} lr={lr}");

            trainer.Train(ids, cfg, onStep: (step, loss) =>
            {
                if (step % Math.Max(1, steps / 20) == 0)
                    Console.WriteLine($"step={step}/{steps} loss={loss:F4}");
            });

            // Save Wout (float32 little-endian)
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
            using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                var w = model.Wout;
                foreach (var f in w) bw.Write(f);
            }
            Console.WriteLine($"[sft] wrote {outPath}");
            return ExitCodes.Ok;
        }

        private static int EstimateVocab(ITokenizer tok)
        {
            // If tokenizer exposes piece count, use it. Otherwise return a safe default.
            // You can extend ITokenizer to expose VocabSize; for now, 50k default is fine for demo.
            return 50_000;
        }
    }
}
