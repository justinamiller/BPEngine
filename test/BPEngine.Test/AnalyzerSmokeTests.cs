using BPEngine.Tokenizer;
using FluentAssertions;

public class AnalyzerSmokeTests
{
    [Fact]
    public void Encode_Lengths_Are_Reasonable()
    {
        var tok = new ByteLevelBPETokenizer("gpt2_merges.txt", VocabJsonReader.Load("vocab.json"), new());
        var line = "This is a moderately long line to check basic encode length behavior.";
        var ids = tok.Encode(line);
        ids.Length.Should().BeGreaterThan(5);
    }
}
