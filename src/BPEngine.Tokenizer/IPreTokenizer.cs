using System.Collections.Generic;

namespace BPEngine.Tokenizer
{
    public interface IPreTokenizer
    {
        IEnumerable<string> Segment(string text);
    }
}