using BPEngine.Tokenizer;
using BPEngine.Transformers;
using FluentAssertions;

public class TinyTransformerShapeTests
{
    [Fact]
    public void Forward_Logits_Has_Expected_Shape()
    {
        var tok = new ByteLevelBPETokenizer("gpt2_merges.txt", VocabJsonReader.Load("vocab.json"), new());
        var ids = tok.Encode("tiny transformer");
        var model = new TinyTransformer(vocabSize: 32000, dim: 32, heads: 2, layers: 1, maxSeq: 32);
        var logits = model.ForwardLogits(ids);
        logits.Length.Should().Be(ids.Length * 32000);
    }
}
