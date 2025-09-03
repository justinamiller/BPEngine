using BPEngine.Tokenizer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    internal class MergesReader
    {
        public static Dictionary<(string, string), int> LoadRanks(string path)
        {
            Guards.FileExists(path, "merges.txt");
            var ranks = new Dictionary<(string, string), int>();
            int rank = 0;
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.IsNullOrWhiteSpace()) continue;
                if (line.StartsWith("#")) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                var key = (parts[0], parts[1]);
                if (!ranks.ContainsKey(key))
                    ranks[key] = rank++;
            }
            if (ranks.Count == 0) throw new InvalidMergesException($"No merges parsed from {path}");
            return ranks;
        }
    }
}
