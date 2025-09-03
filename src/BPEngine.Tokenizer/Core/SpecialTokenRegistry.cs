using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public sealed class SpecialTokenRegistry : ISpecialTokenRegistry
    {
        private readonly Dictionary<string, int> _tok2id;
        private readonly Dictionary<int, string> _id2tok;
        public SpecialTokenRegistry(Dictionary<string, int> map)
        {
            _tok2id = map;
            _id2tok = map.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
        public bool TryGetId(string t, out int id) => _tok2id.TryGetValue(t, out id);
        public bool TryGetToken(int id, out string t) => _id2tok.TryGetValue(id, out t);
        public int Count => _tok2id.Count;
    }

}
