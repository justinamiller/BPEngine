using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BPEngine.Cli.Perf
{
    internal sealed class PerfRun : IDisposable
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly TimeSpan _cpuStart;
        private readonly long _allocStart;
        private readonly long _gen0Start, _gen1Start, _gen2Start;
        private readonly Process _proc;
        private readonly DateTime _t0Utc = DateTime.UtcNow;

        public string Command { get; }
        public string Mode { get; }
        public long Tokens { get; private set; }
        public int Iterations { get; private set; }
        public Dictionary<string, object> Extra { get; } = new();

        public PerfRun(string command, string mode)
        {
            Command = command;
            Mode = mode;

            _proc = Process.GetCurrentProcess();
            _cpuStart = _proc.TotalProcessorTime;
#if NET6_0_OR_GREATER
            _allocStart = GC.GetTotalAllocatedBytes(true);
#else
            _allocStart = 0;
#endif
            _gen0Start = GC.CollectionCount(0);
            _gen1Start = GC.CollectionCount(1);
            _gen2Start = GC.CollectionCount(2);
        }

        public void AddTokens(long n) => Tokens += n;
        public void AddIteration() => Iterations++;

        public PerfResult Finish()
        {
            _sw.Stop();
            _proc.Refresh();
            var cpuEnd = _proc.TotalProcessorTime;
            var peakWs = SafePeakWorkingSet(_proc);

            var gen0 = GC.CollectionCount(0) - _gen0Start;
            var gen1 = GC.CollectionCount(1) - _gen1Start;
            var gen2 = GC.CollectionCount(2) - _gen2Start;

#if NET6_0_OR_GREATER
            var allocated = Math.Max(0, GC.GetTotalAllocatedBytes(true) - _allocStart);
#else
            var allocated = 0L;
#endif
            var wallMs = _sw.Elapsed.TotalMilliseconds;
            var cpuMs = (cpuEnd - _cpuStart).TotalMilliseconds;
            var tps = wallMs > 0 ? Tokens / (wallMs / 1000.0) : 0.0;

            return new PerfResult
            {
                Command = Command,
                Mode = Mode,
                StartTimeUtc = _t0Utc,
                ElapsedMs = wallMs,
                CpuMs = cpuMs,
                Tokens = Tokens,
                Iterations = Iterations,
                TokensPerSec = tps,
                PeakWorkingSetMB = peakWs / (1024.0 * 1024.0),
                AllocatedBytes = allocated,
                GC0 = gen0,
                GC1 = gen1,
                GC2 = gen2,
                Env = PerfEnv.Capture(),
                Extra = new Dictionary<string, object>(Extra)
            };
        }

        public void Dispose() { /* for using-pattern symmetry */ }

        private static long SafePeakWorkingSet(Process p)
        {
            try { return p.PeakWorkingSet64; } catch { return 0; }
        }
    }

    internal sealed class PerfResult
    {
        public string Command { get; set; } = "";
        public string Mode { get; set; } = "";
        public DateTime StartTimeUtc { get; set; }
        public double ElapsedMs { get; set; }
        public double CpuMs { get; set; }
        public long Tokens { get; set; }
        public int Iterations { get; set; }
        public double TokensPerSec { get; set; }
        public double PeakWorkingSetMB { get; set; }
        public long AllocatedBytes { get; set; }
        public int GC0 { get; set; }
        public int GC1 { get; set; }
        public int GC2 { get; set; }
        public PerfEnv Env { get; set; } = new();
        public Dictionary<string, object> Extra { get; set; } = new();
    }

    internal sealed class PerfEnv
    {
        public string OSDescription { get; set; } = RuntimeInformation.OSDescription;
        public string OSArchitecture { get; set; } = RuntimeInformation.OSArchitecture.ToString();
        public string ProcessArchitecture { get; set; } = RuntimeInformation.ProcessArchitecture.ToString();
        public string FrameworkDescription { get; set; } = RuntimeInformation.FrameworkDescription;
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;

        public static PerfEnv Capture() => new PerfEnv();
    }

    internal static class PerfExtensions
    {
        public static bool PerfRequested(this Dictionary<string, List<string>> flags)
            => flags.ContainsKey("--perf") || flags.ContainsKey("--perf-json");

        public static bool PerfJson(this Dictionary<string, List<string>> flags)
            => flags.ContainsKey("--perf-json");
    }
}
