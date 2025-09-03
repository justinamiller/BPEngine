using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public static class RegexPreTokenizer
    {
        private static readonly Regex Rx = new(
            "'s|'t|'re|'ve|'m|'ll|'d| ?\\p{L}+| ?\\p{N}+| ?[^\\s\\p{L}\\p{N}]+|\\s+(?!\\S)|\\s+",
            RegexOptions.Compiled);
        public static IEnumerable<string> Split(string text)
            => Rx.Matches(text).Select(m => m.Value);
    }
}
