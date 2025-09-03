using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer
{
    public class VocabNotFoundException : Exception
    {
        public VocabNotFoundException(string message) : base(message) { }
    }
    public class UnknownTokenIdException : Exception
    {
        public UnknownTokenIdException(int id) : base($"Unknown token id {id}.") { }
    }
    public class InvalidMergesException : Exception
    {
        public InvalidMergesException(string message) : base(message) { }
    }
    public class TokenizerConfigException : Exception
    {
        public TokenizerConfigException(string message) : base(message) { }
    }
}
