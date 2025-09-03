
namespace BPEngine.Trainer
{
    public interface IProgressReporter
    {
        void OnStart(int initialSymbols, int targetMerges);
        void OnMerge(int step, string left, string right, int freq);
        void OnDone(int steps);
    }

    public sealed class ConsoleProgressReporter : IProgressReporter
    {
        public void OnStart(int initialSymbols, int targetMerges)
            => Console.WriteLine($"[BPE Trainer] Initial symbols: {initialSymbols}, target merges: {targetMerges}");

        public void OnMerge(int step, string left, string right, int freq)
            => Console.WriteLine($"[merge {step}] {left}+{right} (freq={freq})");

        public void OnDone(int steps)
            => Console.WriteLine($"[BPE Trainer] Completed {steps} merges.");
    }
}
