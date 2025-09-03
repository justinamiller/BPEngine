using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public sealed class VocabStore : IVocabStore
    {
        private readonly Dictionary<string, int> _tok2id;
        private readonly Dictionary<int, string> _id2tok;

        public VocabStore(Dictionary<string, int> map)
        {
            _tok2id = map;
            _id2tok = map.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
        public bool TryGetId(string token, out int id) => _tok2id.TryGetValue(token, out id);
        public bool TryGetToken(int id, out string token) => _id2tok.TryGetValue(id, out token);
        public IReadOnlyDictionary<string, int> Tokens => _tok2id;
    }

}
