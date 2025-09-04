using BPEngine.Tokenizer.Core;
using System.Diagnostics;
using System.Text;

namespace BPEngine.Tokenizer.Core
{
    /// <summary>
    /// Minimal, greedy "mergeable ranks" tokenizer (TikToken-style).
    /// Strategy: greedy longest-match using a trie over tokens in Ranks; fallback to byte tokens.
    /// NOTE: Provide byte tokens in ranks for robust fallback.
    /// </summary>
    public sealed class TikTokenTokenizer : ITokenizer
    {
        private readonly TikTokenModel _model;
        private readonly IPreTokenizer _pre;
        private readonly ISet<string> _allow;
        private readonly ISet<string> _disallow;
        private readonly TrieNode _root = new();

        private sealed class TrieNode
        {
            public Dictionary<char, TrieNode> Next = new();
            public int? Rank; // token id/rank if this node terminates a token
        }

        public TikTokenTokenizer(TikTokenModel model, TokenizerOptions opt)
        {
            _model = model;
            _pre = opt.Regex == RegexPreset.Cl100k ? new Cl100kPreTokenizer() : new Gpt2PreTokenizer();
            _allow = opt.AllowedSpecial ?? new HashSet<string>();
            _disallow = opt.DisallowedSpecial ?? new HashSet<string> { "all" };

            BuildTrie(model.Ranks);
        }

        private void BuildTrie(Dictionary<string, int> ranks)
        {
            foreach (var kv in ranks)
            {
                var s = kv.Key;
                var cur = _root;
                foreach (var ch in s)
                {
                    if (!cur.Next.TryGetValue(ch, out var nx))
                        cur.Next[ch] = nx = new TrieNode();
                    cur = nx;
                }
                cur.Rank = kv.Value;
            }
        }

        public int[] Encode(string text)
        {
            // Special handling (very simple): if a special is present and allowed, emit it as single token
            if (_disallow.Contains("all") == false && _model.Specials.Count > 0)
            {
                // if caller whitelisted specials explicitly, we honor only those
            }

            var ids = new List<int>();
            foreach (var segment in _pre.Segment(text))
            {
                Greedy(segment, ids);
            }
            return ids.ToArray();
        }

        private void Greedy(string s, List<int> outIds)
        {
            // Greedy longest-match over the trie; fallback per byte.
            for (int i = 0; i < s.Length;)
            {
                int bestRank = -1;
                int bestLen = 0;
                var cur = _root;
                int j = i;

                while (j < s.Length && cur.Next.TryGetValue(s[j], out var nx))
                {
                    cur = nx; j++;
                    if (cur.Rank.HasValue) { bestRank = cur.Rank.Value; bestLen = j - i; }
                }

                if (bestLen > 0)
                {
                    outIds.Add(bestRank);
                    i += bestLen;
                }
                else
                {
                    // Fallback: emit byte token(s). Expect byte tokens exist in ranks (e.g. single-char entries).
                    var ch = s[i];
                    var piece = new string(ch, 1);
                    if (_model.Ranks.TryGetValue(piece, out var r))
                    {
                        outIds.Add(r);
                        i++;
                    }
                    else
                    {
                        // Last-resort: UTF8 bytes → each byte token (must exist in ranks as "\u00XX")
                        var bytes = Encoding.UTF8.GetBytes(piece);
                        foreach (var b in bytes)
                        {
                            var btok = char.ConvertFromUtf32(b);
                            if (!_model.Ranks.TryGetValue(btok, out var br))
                                throw new InvalidOperationException($"No byte fallback token for 0x{b:X2}");
                            outIds.Add(br);
                        }
                        i++;
                    }
                }
            }
        }

        public string Decode(IEnumerable<int> ids)
        {
            // Reconstruct by concatenating token strings (simple path).
            // For exact parity you may need byte/unicode trick; add once your ranks include non-printables.
            var sb = new StringBuilder();
            foreach (var id in ids)
            {
                var tok = _model.Ranks.FirstOrDefault(kv => kv.Value == id).Key;
                if (tok is null) throw new KeyNotFoundException($"Unknown id {id}");
                sb.Append(tok);
            }
            return sb.ToString();
        }
    }
}
