using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public interface ITokenizer
    {
        int[] Encode(string text);
        string Decode(IEnumerable<int> ids);
    }
}
