using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
        public sealed class BpeRanks : IMergesProvider
        {
            public IReadOnlyDictionary<(string Left, string Right), int> Ranks { get; }

            public BpeRanks(Dictionary<(string, string), int> ranks)
            {
                Ranks = ranks;
            }
        }
    }
