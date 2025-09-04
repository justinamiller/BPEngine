using System.Text.Json;

namespace BPEngine.Tokenizer.Core
{
    /// <summary>
    /// TikToken-like "mergeable ranks" model: token(string)->rank(int). Rank also serves as ID.
    /// Specials are optional; pass in explicitly.
    /// </summary>
    public sealed class TikTokenModel
    {
        public Dictionary<string, int> Ranks { get; }
        public Dictionary<string, int> Specials { get; }

        public TikTokenModel(Dictionary<string, int> ranks, Dictionary<string, int>? specials = null)
        {
            Ranks = ranks;
            Specials = specials ?? new();
        }

        public static TikTokenModel LoadFromJson(string ranksPath, string? specialsPath = null)
        {
            var ranks = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(ranksPath))
                        ?? throw new InvalidOperationException("Empty ranks JSON");
            Dictionary<string, int>? specials = null;
            if (!string.IsNullOrWhiteSpace(specialsPath))
            {
                specials = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(specialsPath));
            }
            return new TikTokenModel(ranks, specials);
        }
    }
}
