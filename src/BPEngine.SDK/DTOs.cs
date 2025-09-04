namespace BPEngine.SDK;

/// <summary>
/// Result of tokenizing text into integer IDs.
/// </summary>
/// <param name="Ids">Sequence of token IDs produced by the tokenizer.</param>
public sealed record EncodeResult(int[] Ids);

/// <summary>
/// Result of decoding a sequence of token IDs back into text.
/// </summary>
/// <param name="Text">The reconstructed string from token IDs.</param>
public sealed record DecodeResult(string Text);

/// <summary>
/// Summary of token statistics from corpus analysis.
/// </summary>
/// <param name="TotalTokens">Total number of tokens in the analyzed corpus.</param>
/// <param name="TopTokens">Most frequent individual tokens with their counts.</param>
/// <param name="TopBigrams">Most frequent token pairs (bigrams) with their counts.</param>
public sealed record AnalyzeReport(
    int TotalTokens,
    IReadOnlyList<(string Token, int Count)> TopTokens,
    IReadOnlyList<((string A, string B) Bigram, int Count)> TopBigrams
);

/// <summary>
/// A single retrieval hit from the TF-IDF index.
/// </summary>
/// <param name="DocIndex">Index of the document in the corpus.</param>
/// <param name="Score">Similarity score (higher = closer match).</param>
/// <param name="Snippet">Snippet of the matched document text.</param>
public sealed record RagHit(int DocIndex, double Score, string Snippet);

/// <summary>
/// Result set of retrieval-augmented generation (RAG) query.
/// </summary>
/// <param name="Hits">Collection of top matching documents with scores.</param>
public sealed record RagResult(IReadOnlyList<RagHit> Hits);

/// <summary>
/// Result of training a new BPE tokenizer on a corpus.
/// </summary>
/// <param name="MergesPath">Path to the generated merges.txt file.</param>
/// <param name="VocabPath">Path to the generated vocab.json file.</param>
public sealed record TrainResult(string MergesPath, string VocabPath);

/// <summary>
/// Result of text generation (transformer head or constrained decoding).
/// </summary>
/// <param name="Text">Plain text output from the generator.</param>
/// <param name="Json">Optional JSON output (when using constrained JSON decoding).</param>
public sealed record GenerateResult(string Text, string? Json = null);
