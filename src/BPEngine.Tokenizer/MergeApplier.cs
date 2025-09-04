using BPEngine.Tokenizer.Caching;
using BPEngine.Tokenizer.Performance;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BPEngine.Tokenizer
{
    public static class MergeApplier
    {
        /// <param name="cache">
        /// LRU cache of the space-joined merge result for a token (token -> "p1 p2 ...").
        /// </param>
        internal static IEnumerable<string> Apply(string token,IReadOnlyDictionary<(string, string), int> ranks,LruTokenCache? cache = null,TokenizerDiagnostics diag = null)
        {

            if(diag is null)
            {
                diag = new TokenizerDiagnostics();
            }

            // Cache fast-path
            if (cache is not null && cache.TryGet(token, out var cached))
            {
                diag.MergeCacheHits++;
                return cached.Split(' ');
            }
            diag.MergeCacheMisses++;

            // Split token into "characters"
            var word = new List<string>(capacity: Math.Max(4, token.Length));
            for (int i = 0; i < token.Length; i++)
                word.Add(new string(token[i], 1));

            static List<(string, string)> GetPairs(IList<string> pieces)
            {
                var pairs = new List<(string, string)>(Math.Max(0, pieces.Count - 1));
                for (int i = 0; i < pieces.Count - 1; i++)
                    pairs.Add((pieces[i], pieces[i + 1]));
                return pairs;
            }

            var pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                cache?.Set(token, token);
                return new[] { token };
            }

            while (true)
            {
                (string, string)? best = null;
                int bestRank = int.MaxValue;

                foreach (var p in pairs)
                {
                    if (ranks.TryGetValue(p, out var r) && r < bestRank)
                    {
                        bestRank = r;
                        best = p;
                    }
                }
                if (best is null) break;

                var (a, b) = best.Value;
                var newWord = new List<string>(word.Count);
                int i = 0;
                while (i < word.Count)
                {
                    // find next (a,b)
                    int j = -1;
                    for (int k = i; k < word.Count - 1; k++)
                    {
                        if (word[k] == a && word[k + 1] == b) { j = k; break; }
                    }
                    if (j == -1)
                    {
                        // append tail
                        for (; i < word.Count; i++) newWord.Add(word[i]);
                        break;
                    }
                    // append up to pair
                    for (; i < j; i++) newWord.Add(word[i]);
                    // merge (a+b)
                    newWord.Add(a + b);
                    i = j + 2;
                }

                word = newWord;
                if (word.Count == 1) break;
                pairs = GetPairs(word);
            }

            // Join once and cache
            int totalChars = 0; for (int k = 0; k < word.Count; k++) totalChars += word[k].Length;
            int needed = totalChars + Math.Max(0, word.Count - 1);
            Span<char> initial = needed <= 256 ? stackalloc char[256] : stackalloc char[0];
            var vsb = initial.IsEmpty
                ? new ValueStringBuilder(new Span<char>(new char[256]))
                : new ValueStringBuilder(initial);

            for (int k = 0; k < word.Count; k++)
            {
                if (k > 0) vsb.Append(' ');
                vsb.Append(word[k].AsSpan());
            }
            string joined = vsb.ToString();

            cache?.Set(token, joined);
            return word;
        }
    }
}
