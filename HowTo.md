# How-To Guide for BPEngine Solution

This guide explains how to use the **BPEngine** solution end-to-end:  
from building and training a tokenizer, to analyzing text, to experimenting with N-gram models and the Tiny Transformer playground.

---

## 1. Build the Solution

Clone and build:

```bash
dotnet build
This will build all projects under /src:

BPEngine.Tokenizer (DLL)

BPEngine.Trainer

BPEngine.Models

BPEngine.Transformers

BPEngine.Cli (console tool)

2. Encode and Decode Text
Encode a string into token IDs:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- encode \
  --merges ./data/demo/gpt2_merges.txt \
  --vocab  ./data/demo/vocab.json \
  --text "Hello world"
Decode back into text:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- decode \
  --merges ./data/demo/gpt2_merges.txt \
  --vocab  ./data/demo/vocab.json \
  --ids 15496,995
3. Train a New Tokenizer
Train merges and vocab from your own corpus:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- train \
  --corpus ./data/corpus.txt \
  --out ./artifacts \
  --vocab-size 5000 \
  --min-pair 2
This produces:

./artifacts/merges.txt

./artifacts/vocab.json

4. Analyze a Corpus
Get statistics on token usage:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- analyze \
  --corpus ./data/corpus.txt \
  --merges ./artifacts/merges.txt \
  --vocab  ./artifacts/vocab.json \
  --top 20 --bins 10
Outputs include:

Number of tokens per line

Histogram

Top tokens and bigrams

Optional JSON (--perf-json) for dashboards

5. Work with N-Gram Models
Train a 3-gram model:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- ngram train \
  --order 3 \
  --corpus ./data/corpus.txt \
  --merges ./artifacts/merges.txt \
  --vocab  ./artifacts/vocab.json \
  --out ./artifacts/ngram.json
Generate text:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- ngram generate \
  --order 3 \
  --model ./artifacts/ngram.json \
  --merges ./artifacts/merges.txt \
  --vocab ./artifacts/vocab.json \
  --prompt "Hello" \
  --max 50
6. Experiment with the Tiny Transformer
Run a forward-pass generation with random weights:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- transformer demo \
  --merges ./data/demo/gpt2_merges.txt \
  --vocab  ./data/demo/vocab.json \
  --prompt "Hello world" \
  --layers 2 --heads 2 --dim 64 --max-seq 64 \
  --max-new 30 --temp 1.0 --topk 0
This will generate new tokens and decode them into text.
Note: output is random and not meaningful, but demonstrates the full pipeline.

7. Use as a Library
Reference the BPEngine.Tokenizer DLL in any C# project:

csharp
Copy code
using BPEngine.Tokenizer;

var tok = new ByteLevelBPETokenizer(
    mergesPath: "merges.txt",
    tokenToId: VocabJsonReader.Load("vocab.json"),
    specialTokenToId: new Dictionary<string,int> { ["<|bos|>"] = 0, ["<|eos|>"] = 1 }
);

int[] ids = tok.Encode("Hello world");
string text = tok.Decode(ids);
8. Performance Metrics
Enable performance tracking in CLI:

bash
Copy code
dotnet run --project ./src/BPEngine.Cli -- encode \
  --merges merges.txt \
  --vocab vocab.json \
  --text "Hello world" \
  --perf
Outputs:

Tokens per second

Wall and CPU time

Memory and GC counts

Or machine-readable JSON:

bash
Copy code
--perf-json
9. Test and Benchmark
Run unit tests:

bash
Copy code
dotnet test ./tests/BPEngine.Tests
Run benchmarks:

bash
Copy code
dotnet run --project ./bench/BPEngine.Bench -c Release
Benchmarks report tokens per second, allocations, and memory usage.

10. Roadmap for Extensions
Package libraries into NuGet (dotnet pack)

Add REST API server (BPEngine.Server) to expose /encode, /decode, /analyze

Integrate ONNX Runtime for pretrained models

Expand Transformer with trainable weights