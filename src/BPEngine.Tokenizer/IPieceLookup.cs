using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    /// <summary>
    /// Optional fast path: O(1) ID → token piece.
    /// </summary>
    public interface IPieceLookup
    {
        string GetPiece(int id);
    }
}
