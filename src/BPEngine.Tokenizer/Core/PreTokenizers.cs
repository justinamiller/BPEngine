using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BPEngine.Tokenizer.Core
{
    public sealed class Gpt2PreTokenizer : IPreTokenizer
    {
        // Classic GPT-2 segmentation
        // Ref: 's|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+
        static readonly Regex Rx = new(
            @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
            RegexOptions.Compiled);

        public IEnumerable<string> Segment(string text)
        {
            foreach (Match m in Rx.Matches(text)) yield return m.Value;
        }
    }
}
