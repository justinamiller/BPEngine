namespace BPEngine.Tokenizer.Core
{
    using global::BPEngine.Tokenizer.Performance;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// GPT-2/3 style byte-level BPE tokenizer that composes the internal helpers:
    /// - RegexPreTokenizer for pre-tokenization
    /// - ByteUnicodeMap for fast bytes↔unicode mapping
    /// - MergeApplier for BPE merging with optional caching & diagnostics
    /// - MergesReader for pair→rank loading
    /// - Optional vocab.json and special tokens
    ///
    /// Public surface stays minimal: Encode / Decode.
    /// </summary>
    public sealed class ByteLevelBPETokenizer : ITokenizer
    {
        // Static map reused across instances (safe, deterministic)
        private static readonly ByteUnicodeMap _map = ByteUnicodeMap.Build();

        private readonly Dictionary<(string, string), int> _ranks;
        private readonly Dictionary<string, int> _tokenToId;   // includes specials + vocab + dynamic
        private readonly Dictionary<int, string> _idToToken;   // reverse map for decode
        private readonly SpecialTokenRegistry _specials;

        private readonly TokenCache _cache = new();
        private readonly TokenizerDiagnostics _diag = new();

        private readonly TokenizerOptions _opts;

        /// <param name="mergesPath">Path to merges.txt</param>
        /// <param name="tokenToId">Optional vocab.json (token -> id). If null, IDs are assigned dynamically.</param>
        /// <param name="specialTokenToId">Optional map of special tokens (token -> id), e.g., &lt;|bos|&gt;, &lt;|eos|&gt;.</param>
        /// <param name="options">Optional tokenizer options.</param>
        public ByteLevelBPETokenizer(
            string mergesPath,
            IDictionary<string, int>? tokenToId = null,
            IDictionary<string, int>? specialTokenToId = null,
            TokenizerOptions? options = null)
        {
            _opts = options ?? new TokenizerOptions();

            // Load BPE ranks (pair -> rank)
            _ranks = MergesReader.LoadRanks(mergesPath);

            // Build vocab maps
            _tokenToId = tokenToId is not null
                ? new Dictionary<string, int>(tokenToId)
                : new Dictionary<string, int>();

            _idToToken = _tokenToId.Count > 0
                ? _tokenToId.ToDictionary(kv => kv.Value, kv => kv.Key)
                : new Dictionary<int, string>();

            // Specials
            var specials = specialTokenToId is not null ? new Dictionary<string, int>(specialTokenToId)
                                                        : new Dictionary<string, int>();
            _specials = new SpecialTokenRegistry(specials);
        }

        // ----------------------- Encode -----------------------
        public int[] Encode(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var ids = new List<int>(capacity: Math.Max(16, text.Length / 2));

            foreach (var tok in RegexPreTokenizer.Split(text))
            {
                // Exact special token passthrough
                if (_specials.TryGetId(tok, out var sid))
                {
                    ids.Add(sid);
                    continue;
                }

                // UTF-8 bytes -> byte-level-unicode chars
                var bytes = Encoding.UTF8.GetBytes(tok);
                var mappedChars = new char[bytes.Length];
                for (int i = 0; i < bytes.Length; i++)
                    mappedChars[i] = _map.ByteToChar[bytes[i]];

                var mapped = new string(mappedChars);

                // Apply BPE merges
                var pieces = MergeApplier.Apply(mapped, _ranks, _cache, _diag);

                // Resolve piece -> id
                foreach (var piece in pieces)
                    ids.Add(GetOrAssignId(piece));
            }

            if (_opts.MaxLength is int max && ids.Count > max)
                ids.RemoveRange(max, ids.Count - max);

            return ids.ToArray();
        }

        // ----------------------- Decode -----------------------
        public string Decode(IEnumerable<int> tokenIds)
        {
            if (tokenIds is null) throw new ArgumentNullException(nameof(tokenIds));

            var sb = new StringBuilder(capacity: 256);

            foreach (var id in tokenIds)
            {
                // If it's a special, write it literally
                if (_specials.TryGetToken(id, out var sTok))
                {
                    sb.Append(sTok);
                    continue;
                }

                if (!_idToToken.TryGetValue(id, out var piece))
                {
                    if (_opts.ThrowOnUnknownId)
                        throw new UnknownTokenIdException(id);
                    continue;
                }

                // piece is in byte-level-unicode space; map back to UTF-8
                var len = piece.Length;
                var tmp = new byte[len];
                for (int i = 0; i < len; i++)
                    tmp[i] = _map.CharToByte[piece[i]];

                sb.Append(Encoding.UTF8.GetString(tmp));
            }

            return sb.ToString();
        }

        // ----------------------- Internals -----------------------
        private int GetOrAssignId(string piece)
        {
            if (_tokenToId.TryGetValue(piece, out var id))
                return id;

            // Assign after all existing entries (vocab + specials)
            int next = _tokenToId.Count + _specials.Count;
            _tokenToId[piece] = next;
            _idToToken[next] = piece;
            return next;
        }
    }
}
