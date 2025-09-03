
using System.Collections.Concurrent;

namespace BPEngine.Trainer
{
    /// <summary>
    /// Counts adjacent pair frequencies across a sequence of tokens,
    /// where each token is already mapped to byte-level unicode and represented as string.
    /// </summary>
    public sealed class PairCounter
    {
        public Dictionary<(string,string), int> CountPairs(IEnumerable<string> mappedTokens)
        {
            var dict = new Dictionary<(string,string), int>(capacity: 1<<16);
            foreach (var tok in mappedTokens)
            {
                if (tok.Length < 2) continue;
                // token -> list of "characters" (one-char strings)
                var chars = tok.Select(ch => ch.ToString()).ToArray();
                for (int i = 0; i < chars.Length - 1; i++)
                {
                    var key = (chars[i], chars[i+1]);
                    dict.TryGetValue(key, out var c);
                    dict[key] = c + 1;
                }
            }
            return dict;
        }
    }
}
