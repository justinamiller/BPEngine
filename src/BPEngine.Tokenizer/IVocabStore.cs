using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public interface IVocabStore
    {
        bool TryGetId(string token, out int id);
        bool TryGetToken(int id, out string token);
        IReadOnlyDictionary<string, int> Tokens { get; }
    }
}
