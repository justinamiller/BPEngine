using BPEngine.Tokenizer;
using FluentAssertions;

public class TokenizerRoundTripTests
{
    [Fact]
    public void RoundTrip_Ascii_And_Emoji()
    {
        var tok = new ByteLevelBPETokenizer("gpt2_merges.txt", VocabJsonReader.Load("vocab.json"), new());
        var texts = new[]
        {
            "Hello world",
            "Café déjà vu",
            "rocket 🚀 goes brrr",
            "tabs\tand\nnewlines"
        };
        foreach (var t in texts)
        {
            var ids = tok.Encode(t);
            var back = tok.Decode(ids);
            back.Should().Be(t);
        }
    }
}
