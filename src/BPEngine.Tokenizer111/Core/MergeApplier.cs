using BPEngine.Tokenizer.Core;
using BPEngine.Tokenizer.Performance;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BPEngine.Tokenizer.Core
{
    /// <summary>
    /// Applies byte-level BPE merges to a single token (already mapped into byte-unicode space).
    /// - Uses a rank table (pair -> rank).
    /// - Caches the space-joined result string (e.g., "he ll o") to avoid recomputation.
    /// - On cache hit: splits the cached string back into pieces (no re-merge cost).
    /// - Builds the cached string with ValueStringBuilder to reduce allocations.
    /// </summary>
    public static class MergeApplier
    {
        /// <param name="token">A single token in byte-unicode space</param>
        /// <param name="ranks">Pair -> rank map (lower rank = higher priority)</param>
        /// <param name="cache">Optional merge result cache; caches the joined string "p1 p2 ..."</param>
        /// <param name="diag">Optional diagnostics (cache hits/misses)</param>
        public static IEnumerable<string> Apply(
            string token,
            IReadOnlyDictionary<(string, string), int> ranks,
            TokenCache? cache = null,
            TokenizerDiagnostics? diag = null)
        {
            // Fast path: cache lookup
            if (cache is not null && cache.TryGet(token, out var cached))
            {
                diag?.MergeCacheHits++;
                // Split on single space — we only ever join with ' ' below
                return cached.Split(' ');
            }
            diag?.MergeCacheMisses++;

            // Split token into "characters" (each piece is a one-char string)
            // NOTE: We keep strings here because ranks are on string pairs.
            var word = new List<string>(capacity: Math.Max(4, token.Length));
            for (int i = 0; i < token.Length; i++)
                word.Add(new string(token[i], 1));

            // Local helpers for pair operations
            static List<(string, string)> GetPairs(IList<string> pieces)
            {
                var pairs = new List<(string, string)>(Math.Max(0, pieces.Count - 1));
                for (int i = 0; i < pieces.Count - 1; i++)
                    pairs.Add((pieces[i], pieces[i + 1]));
                return pairs;
            }

            static int IndexOfPair(IList<string> pieces, string a, string b, int start)
            {
                for (int i = start; i < pieces.Count - 1; i++)
                {
                    if (pieces[i] == a && pieces[i + 1] == b) return i;
                }
                return -1;
            }

            // Initial pairs
            var pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                // Cache the trivial case (single piece == whole token)
                cache?.Set(token, token);
                return new[] { token };
            }

            // BPE loop: iteratively merge best-ranked bigram
            while (true)
            {
                (string, string)? best = null;
                int bestRank = int.MaxValue;

                // Find the lowest-rank (highest-priority) pair present
                foreach (var p in pairs)
                {
                    if (ranks.TryGetValue(p, out var r) && r < bestRank)
                    {
                        bestRank = r;
                        best = p;
                    }
                }

                if (best is null) break; // no more applicable merges

                var (a, b) = best.Value;

                // Merge occurrences of (a,b) across the word
                var newWord = new List<string>(word.Count);
                int i = 0;
                while (i < word.Count)
                {
                    int j = IndexOfPair(word, a, b, i);
                    if (j == -1)
                    {
                        // append the tail
                        for (; i < word.Count; i++) newWord.Add(word[i]);
                        break;
                    }

                    // append everything up to the pair
                    for (; i < j; i++) newWord.Add(word[i]);

                    // merge (a+b)
                    newWord.Add(a + b);
                    i = j + 2; // skip over the merged pair
                }

                word = newWord;

                if (word.Count == 1) break;     // fully merged
                pairs = GetPairs(word);         // rebuild pairs for next iteration
            }

            // Build the cached space-joined result efficiently:
            // "piece0 piece1 piece2 ..."
            // Estimate required chars to reduce reallocations:
            int totalChars = 0;
            for (int k = 0; k < word.Count; k++) totalChars += word[k].Length;
            int needed = totalChars + Math.Max(0, word.Count - 1); // spaces between

            // Use stackalloc for small strings; ValueStringBuilder grows into pooled arrays if needed.
            Span<char> initial = needed <= 256 ? stackalloc char[256] : stackalloc char[0];
            var vsb = initial.IsEmpty
                ? new ValueStringBuilder(new Span<char>(new char[256])) // start with 256 if needed > 256
                : new ValueStringBuilder(initial);

            for (int k = 0; k < word.Count; k++)
            {
                if (k > 0) vsb.Append(' ');
                vsb.Append(word[k].AsSpan());
            }

            string joined = vsb.ToString(); // also disposes pooled buffer if it grew
            cache?.Set(token, joined);

            return word;
        }
    }
}
