using System.Collections.Generic;

namespace BPEngine.Tokenizer.Caching
{
    /// <summary>
    /// Thread-safe, O(1) LRU cache specialized for tokenizer hot paths.
    /// Key and value are strings (e.g., token -> "p1 p2 ..." or piece -> decoded UTF-8).
    /// </summary>
    internal sealed class LruTokenCache
    {
        private readonly int _capacity;
        private readonly Dictionary<string, LinkedListNode<Entry>> _map;
        private readonly LinkedList<Entry> _list;
        private readonly object _gate = new();

        private sealed class Entry
        {
            public string Key = "";
            public string Value = "";
        }

        public int Capacity => _capacity;
        public int Count { get { lock (_gate) return _map.Count; } }

        public LruTokenCache(int capacity = 50_000)
        {
            if (capacity <= 0) capacity = 1;
            _capacity = capacity;
            _map = new Dictionary<string, LinkedListNode<Entry>>(capacity);
            _list = new LinkedList<Entry>();
        }

        public bool TryGet(string key, out string value)
        {
            lock (_gate)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    // move to front (MRU)
                    _list.Remove(node);
                    _list.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                value = default!;
                return false;
            }
        }

        public void Set(string key, string value)
        {
            lock (_gate)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    node.Value.Value = value;
                    _list.Remove(node);
                    _list.AddFirst(node);
                    return;
                }

                // insert new
                var entry = new Entry { Key = key, Value = value };
                var newNode = new LinkedListNode<Entry>(entry);
                _list.AddFirst(newNode);
                _map[key] = newNode;

                // evict if needed
                if (_map.Count > _capacity)
                {
                    var lru = _list.Last;
                    if (lru is not null)
                    {
                        _map.Remove(lru.Value.Key);
                        _list.RemoveLast();
                    }
                }
            }
        }
    }
}
