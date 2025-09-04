using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public sealed class Cl100kPreTokenizer : IPreTokenizer
    {
        // Approximate cl100k behavior (slightly different around punctuation/whitespace).
        // This is a good default; refine later with more test cases.
        static readonly Regex Rx = new(
            @"\p{Zs}+|[\u0000-\u001F]+|[A-Za-z0-9]+(?:['’][A-Za-z]+)?|[^\p{Zs}\u0000-\u001F]+",
            RegexOptions.Compiled);

        public IEnumerable<string> Segment(string text)
        {
            foreach (Match m in Rx.Matches(text)) yield return m.Value;
        }
    }
}
