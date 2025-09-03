using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public interface ISpecialTokenRegistry
    {
        bool TryGetId(string token, out int id);
        bool TryGetToken(int id, out string token);
        int Count { get; }
    }
}
