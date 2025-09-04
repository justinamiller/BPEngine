using BPEngine.Tokenizer;
using BPEngine.Transformers;

namespace BPEngine.Cli.Commands
{
    internal static class TransformerHeadCommands
    {
        public static int TrainHead(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);
            var merges = flags.Require("--merges");
            var vocabPath = flags.Optional("--vocab");
            var corpusPath = flags.Require("--corpus");
            var outPath = flags.Optional("--out") ?? "wout.bin";

            int dim = int.TryParse(flags.Optional("--dim"), out var D) ? D : 64;
            int heads = int.TryParse(flags.Optional("--heads"), out var H) ? H : 2;
            int layers = int.TryParse(flags.Optional("--layers"), out var L) ? L : 2;
            int maxSeq = int.TryParse(flags.Optional("--max-seq"), out var MS) ? MS : 64;

            int batch = int.TryParse(flags.Optional("--batch"), out var B) ? B : 8;
            int steps = int.TryParse(flags.Optional("--steps"), out var S) ? S : 500;
            int seqlen = int.TryParse(flags.Optional("--seqlen"), out var SL) ? SL : 64;
            float lr = float.TryParse(flags.Optional("--lr"), out var LR) ? LR : 1e-2f;
            float wd = float.TryParse(flags.Optional("--wd"), out var WD) ? WD : 0.0f;

            var vocab = string.IsNullOrWhiteSpace(vocabPath) ? null : VocabJsonReader.Load(vocabPath);
            int vocabSize = vocab?.Count ?? 32000;
            var tok = new ByteLevelBPETokenizer(merges, vocab, null,new());

            // Load corpus, tokenize into one big stream
            var text = File.ReadAllText(corpusPath);
            var ids = tok.Encode(text);

            // Build model and trainer
            var model = new TinyTransformer(vocabSize, dim, heads, layers, maxSeq);
            var trainer = new HeadTrainer(model);

            var cfg = new HeadTrainer.TrainConfig(BatchSize: batch, SeqLen: seqlen, Steps: steps, LR: lr, WeightDecay: wd);
            trainer.Train(ids, cfg, (step, loss) =>
            {
                if (step % 10 == 0) Console.Error.WriteLine($"step {step}/{steps} loss={loss:0.4f}");
            });

            HeadIo.SaveWout(model, outPath);
            Console.WriteLine($"Saved head weights to {outPath}");
            return ExitCodes.Ok;
        }

        public static int GenerateWithHead(string[] args)
        {
            var (flags, _) = ArgParser.Parse(args);
            var merges = flags.Require("--merges");
            var vocabPath = flags.Optional("--vocab");
            var woutPath = flags.Require("--wout");
            var prompt = flags.Optional("--prompt") ?? "";
            int maxNew = int.TryParse(flags.Optional("--max-new"), out var MN) ? MN : 80;

            int dim = int.TryParse(flags.Optional("--dim"), out var D) ? D : 64;
            int heads = int.TryParse(flags.Optional("--heads"), out var H) ? H : 2;
            int layers = int.TryParse(flags.Optional("--layers"), out var L) ? L : 2;
            int maxSeq = int.TryParse(flags.Optional("--max-seq"), out var MS) ? MS : 64;
            float temp = float.TryParse(flags.Optional("--temp"), out var T) ? T : 0.9f;
            int topK = int.TryParse(flags.Optional("--topk"), out var K) ? K : 40;

            var vocab = string.IsNullOrWhiteSpace(vocabPath) ? null : VocabJsonReader.Load(vocabPath);
            int vocabSize = vocab?.Count ?? 32000;
            var tok = new ByteLevelBPETokenizer(merges, vocab, null, new());

            var model = new TinyTransformer(vocabSize, dim, heads, layers, maxSeq);
            HeadIo.LoadWout(model, woutPath);

            // Sampling loop (uses model.ForwardLogits)
            var ids = prompt.Length == 0 ? new List<int>() : tok.Encode(prompt).ToList();
            var rng = new Random(42);

            for (int i = 0; i < maxNew; i++)
            {
                var ctx = ids.Count > maxSeq ? ids.Skip(ids.Count - maxSeq).ToArray() : ids.ToArray();
                var logits = model.ForwardLogits(ctx);
                int next = SampleLast(logits, model.VocabSize, temp, topK, rng);
                ids.Add(next);
            }

            Console.WriteLine(tok.Decode(ids));
            return ExitCodes.Ok;
        }

        private static int SampleLast(float[] logits, int vocab, float temperature, int topK, Random rng)
        {
            var probs = logits.AsSpan(logits.Length - vocab, vocab).ToArray();
            if (temperature <= 0) temperature = 1e-6f;
            for (int i = 0; i < probs.Length; i++) probs[i] /= temperature;

            if (topK > 0 && topK < vocab)
            {
                var idx = Enumerable.Range(0, vocab).OrderByDescending(i => probs[i]).Take(topK).ToHashSet();
                for (int i = 0; i < vocab; i++) if (!idx.Contains(i)) probs[i] = float.NegativeInfinity;
            }

            // softmax + sample
            float max = probs.Max();
            double sum = 0; for (int i = 0; i < probs.Length; i++) { var e = Math.Exp(probs[i] - max); probs[i] = (float)e; sum += e; }
            var r = rng.NextDouble() * sum;
            double acc = 0;
            for (int i = 0; i < probs.Length; i++) { acc += probs[i]; if (r <= acc) return i; }
            return vocab - 1;
        }
    }
}
