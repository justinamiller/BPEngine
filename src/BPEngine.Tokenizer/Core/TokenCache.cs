using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    /// <summary>Simple unbounded token cache. Replace with LRU if needed.</summary>
    public sealed class TokenCache
    {
        private readonly Dictionary<string, string> _cache = new();
        public bool TryGet(string key, out string value) => _cache.TryGetValue(key, out value);
        public void Set(string key, string value) => _cache[key] = value;
    }
}
