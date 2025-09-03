namespace BPEngine.Tokenizer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Minimal GPT-2 style byte-level BPE tokenizer in C#.
    /// - Load merges (pair -> rank) from a merges.txt (first 50 lines are header in GPT-2; skip them).
    /// - Optional: load vocab.json (token string -> id). If absent, ids come from rank order.
    /// - Supports basic special tokens.
    /// </summary>
    public sealed class ByteLevelBPETokenizer
    {
        // --- Configuration ---
        private readonly Dictionary<(string, string), int> _bpeRanks;
        private readonly Dictionary<string, int> _tokenToId;   // optional if you have vocab.json
        private readonly Dictionary<int, string> _idToToken;   // optional reverse map
        private readonly Dictionary<string, int> _specialTokenToId;
        private readonly Dictionary<int, string> _idToSpecialToken;

        private readonly Dictionary<string, string> _cache = new();
        private readonly Dictionary<byte, char> _byteToUnicode;
        private readonly Dictionary<char, byte> _unicodeToByte;

        // Regex very close to GPT-2’s
        private static readonly Regex _preTokenize = new Regex(
            @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
            RegexOptions.Compiled
        );

        public ByteLevelBPETokenizer(
            string mergesPath,
            IDictionary<string, int>? tokenToId = null,
            IDictionary<string, int>? specialTokenToId = null)
        {
            // 1) bytes↔unicode maps
            (_byteToUnicode, _unicodeToByte) = BytesToUnicode();

            // 2) load BPE ranks (pair -> rank)
            _bpeRanks = LoadMergesAsRanks(mergesPath);

            // 3) vocab (optional). If not supplied, synthesize ids from tokens encountered during encoding.
            _tokenToId = tokenToId is not null ? new Dictionary<string, int>(tokenToId)
                                               : new Dictionary<string, int>();
            _idToToken = _tokenToId.Count > 0 ? _tokenToId.ToDictionary(kv => kv.Value, kv => kv.Key)
                                              : new Dictionary<int, string>();

            // 4) special tokens
            _specialTokenToId = specialTokenToId is not null ? new Dictionary<string, int>(specialTokenToId)
                                                             : new Dictionary<string, int>();
            _idToSpecialToken = _specialTokenToId.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        // ------------------- Public API -------------------

        public int[] Encode(string text, bool addSpecialTokens = false)
        {
            // If you have special tokens like <|bos|>, <|eot|>, you can inject them here.
            var ids = new List<int>();

            foreach (Match m in _preTokenize.Matches(text))
            {
                string token = m.Value;

                // Shortcut: exact special token?
                if (_specialTokenToId.TryGetValue(token, out int sid))
                {
                    ids.Add(sid);
                    continue;
                }

                // 1) byte-encode then map bytes→unicode (so every byte is printable)
                var bytes = Encoding.UTF8.GetBytes(token);
                var mapped = new string(bytes.Select(b => _byteToUnicode[b]).ToArray());

                // 2) BPE over the unicode-mapped string
                foreach (var piece in BPE(mapped))
                {
                    int id = GetOrAssignId(piece);
                    ids.Add(id);
                }
            }

            return ids.ToArray();
        }

        public string Decode(IEnumerable<int> tokenIds)
        {
            var sb = new StringBuilder();
            foreach (int id in tokenIds)
            {
                if (_idToSpecialToken.TryGetValue(id, out string sTok))
                {
                    sb.Append(sTok);
                    continue;
                }

                string piece = _idToToken.TryGetValue(id, out var t)
                    ? t
                    : throw new InvalidOperationException($"Unknown token id {id} (no vocab.json loaded and id not assigned).");

                // BPE pieces are in the unicode-space; map back to raw bytes, then UTF-8 decode
                var bytes = piece.Select(ch => _unicodeToByte[ch]).ToArray();
                sb.Append(Encoding.UTF8.GetString(bytes));
            }
            return sb.ToString();
        }

        // ------------------- Internals -------------------

        private static (Dictionary<byte, char>, Dictionary<char, byte>) BytesToUnicode()
        {
            // Matches OpenAI tiktoken/byte-level BPE approach: map 0..255 into distinct unicode codepoints
            List<int> bs = Enumerable.Range(33, 94).Concat(
                           Enumerable.Range(161, 95)).ToList();
            var cs = new List<int>(bs);
            int n = 0;
            for (int b = 0; b < 256; b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add(256 + n);
                    n++;
                }
            }

            var byteToUni = new Dictionary<byte, char>(256);
            var uniToByte = new Dictionary<char, byte>(256);

            for (int i = 0; i < bs.Count; i++)
            {
                byteToUni[(byte)bs[i]] = (char)cs[i];
                uniToByte[(char)cs[i]] = (byte)bs[i];
            }

            return (byteToUni, uniToByte);
        }

        private static Dictionary<(string, string), int> LoadMergesAsRanks(string mergesPath)
        {
            // GPT-2 merges.txt starts with a header like:
            // #version: 0.2
            // and then one pair per line: "t h", "th e", ...
            var ranks = new Dictionary<(string, string), int>();
            int rank = 0;

            foreach (var line in File.ReadLines(mergesPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;

                ranks[(parts[0], parts[1])] = rank++;
            }
            return ranks;
        }

        private IEnumerable<string> BPE(string token)
        {
            if (_cache.TryGetValue(token, out var cached))
                return cached.Split(' ');

            // Split token into a list of characters (in unicode-space)
            var word = token.Select(ch => ch.ToString()).ToList();

            // Build initial pairs
            var pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                _cache[token] = token;
                return new[] { token };
            }

            while (true)
            {
                // Pick best (lowest-rank) pair
                (string, string)? bigram = null;
                int bestRank = int.MaxValue;

                foreach (var p in pairs)
                {
                    if (_bpeRanks.TryGetValue(p, out int r) && r < bestRank)
                    {
                        bestRank = r;
                        bigram = p;
                    }
                }

                if (bigram is null) break;

                var (first, second) = bigram.Value;
                var newWord = new List<string>();
                int i = 0;
                while (i < word.Count)
                {
                    int j = IndexOfPair(word, first, second, i);
                    if (j == -1)
                    {
                        newWord.AddRange(word.Skip(i));
                        break;
                    }
                    newWord.AddRange(word.Skip(i).Take(j - i));
                    newWord.Add(first + second);
                    i = j + 2;
                }

                word = newWord;
                if (word.Count == 1) break;
                pairs = GetPairs(word);
            }

            var result = string.Join(' ', word);
            _cache[token] = result;
            return word;
        }

        private static List<(string, string)> GetPairs(IList<string> word)
        {
            var pairs = new List<(string, string)>();
            for (int i = 0; i < word.Count - 1; i++)
                pairs.Add((word[i], word[i + 1]));
            return pairs;
        }

        private static int IndexOfPair(IList<string> word, string a, string b, int start)
        {
            for (int i = start; i < word.Count - 1; i++)
            {
                if (word[i] == a && word[i + 1] == b) return i;
            }
            return -1;
        }

        private int GetOrAssignId(string piece)
        {
            if (_tokenToId.TryGetValue(piece, out int id))
                return id;

            // If there’s no vocab.json, synthesize ids deterministically:
            // base range after special tokens
            int nextId = _tokenToId.Count + _specialTokenToId.Count;
            _tokenToId[piece] = nextId;
            _idToToken[nextId] = piece;
            return nextId;
        }
    }
}
