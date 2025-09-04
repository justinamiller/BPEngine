using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPEngine.Tokenizer;

namespace BPEngine.SDK;

internal static class TokenStatsAnalyzer
{
    internal sealed record Stats(
        int TotalTokens,
        List<(string Token, int Count)> TopTokens,
        List<((string A, string B) Bigram, int Count)> TopBigrams);

    public static Stats Analyze(ITokenizer tok, string corpusPath, int top = 20)
    {
        var text = File.ReadAllText(corpusPath);
        var ids = tok.Encode(text);
        var freq = new Dictionary<int, int>();
        var big = new Dictionary<(int, int), int>();

        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            freq[id] = freq.TryGetValue(id, out var c) ? c + 1 : 1;
            if (i + 1 < ids.Length)
            {
                var k = (ids[i], ids[i + 1]);
                big[k] = big.TryGetValue(k, out var bc) ? bc + 1 : 1;
            }
        }

        string Piece(int id) =>
            tok is IPieceLookup pl ? pl.GetPiece(id) : tok.Decode(new[] { id });

        var topTokens = freq
            .OrderByDescending(kv => kv.Value)
            .Take(top)
            .Select(kv => (Piece(kv.Key), kv.Value))
            .ToList();

        var topBigrams = big
            .OrderByDescending(kv => kv.Value)
            .Take(top)
            .Select(kv => ((Piece(kv.Key.Item1), Piece(kv.Key.Item2)), kv.Value))
            .ToList();

        return new Stats(ids.Length, topTokens, topBigrams);
    }
}
