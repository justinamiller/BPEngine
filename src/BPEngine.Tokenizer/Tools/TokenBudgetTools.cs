using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{    public static class TokenBudgetTools
    {
        /// <summary>
        /// Trim text so that encoding fits within a token budget. Trims on word boundaries when possible.
        /// Returns the snippet and tokens used.
        /// </summary>
        public static (string Snippet, int TokensUsed) TrimToBudget(ITokenizer tokenizer, string text, int budget, string ellipsis = "…")
        {
            if (budget <= 0 || text.IsNullOrEmpty())
                return ("", 0);

            // Fast path: already fits
            var ids = tokenizer.Encode(text);
            if (ids.Length <= budget) return (text, ids.Length);

            // Binary search over words
            var words = SplitWords(text);
            int lo = 0, hi = words.Count; // length in words
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                var candidate = string.Join("", words.Take(mid));
                var tokCount = tokenizer.Encode(candidate).Length + 1; // reserve for ellipsis
                if (tokCount <= budget) lo = mid; else hi = mid - 1;
            }
            var finalText = string.Join("", words.Take(lo)) + ellipsis;
            var used = tokenizer.Encode(finalText).Length;
            return (finalText, used);
        }

        private static List<string> SplitWords(string s)
        {
            // Split but keep separators so we don’t destroy spacing/punctuation
            var list = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                int j = i;
                while (j < s.Length && !char.IsWhiteSpace(s[j])) j++;
                if (j > i) list.Add(s[i..j]);
                int k = j;
                while (k < s.Length && char.IsWhiteSpace(s[k])) k++;
                if (k > j) list.Add(s[j..k]);
                i = k;
            }
            return list;
        }
    }

}
