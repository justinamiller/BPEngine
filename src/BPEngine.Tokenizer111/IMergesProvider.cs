using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public interface IMergesProvider
    {
        IReadOnlyDictionary<(string Left, string Right), int> Ranks { get; }
    }
}
