# BPEngine

BPEngine is a **pure C#** implementation of GPT-style **byte-pair encoding (BPE) tokenization** and a playground for lightweight generative models.  
It now includes support for **next-gen GPT-3.5/4-style tokenizers (cl100k)**, constrained decoding, and retrieval.

The long-term goal: a self-contained .NET solution for the **full lifecycle of text processing**:  
**train → analyze → package → serve → integrate → constrain → retrieve → generate**.

---

## Why BPEngine?

Most open-source tokenizers and GPT demos rely on Python. BPEngine is for teams that standardize on .NET and want:

- **Tokenizer parity** with GPT-2/3 *and* GPT-3.5/4 (cl100k).
- **Zero Python** across training, runtime, and tooling.
- **Deterministic + testable** behavior for enterprise workloads.
- **Educational + practical** modeling tools that run on CPU.

---

## Features

### Tokenization
- GPT-2/3 style byte-level BPE (merges + vocab).
- GPT-3.5/4 style cl100k tokenizer (mergeable ranks).
- Regex pre-tokenizers: `Gpt2Regex`, `Cl100kRegex`.
- Special tokens: allowed/disallowed sets.
- Snippet budgeting: enforce token budgets.

### Training
- Train merges + vocab from corpus.
- Corpus analysis: token histograms, bigrams, distributions.
- Seed corpus trick for domain tokens.

### Lightweight Models
- N-gram model: simple baseline generator.
- Tiny Transformer demo: forward-only GPT-like playground.
- Trainable Transformer Head: update weights on your corpus.

### Next-Gen Additions
- Constrained JSON decoding: always-valid JSON outputs.
- Tool calling hooks: schema + function router interface.
- TF-IDF Retriever: simple retrieval-augmented generation.

---

## Architecture

```
[ corpus.txt ]
      │
      ▼
[ BPE Trainer ]  ──►  merges.txt + vocab.json / ranks.json
                                                 │
                                          [ BPEngine.Tokenizer ]
                                                 │
      ┌──────────────────────────┬──────────────────────────┐
      ▼                          ▼                          ▼
 [ Corpus Analyzer ]     [ Lightweight Models ]       [ CLI / Tooling ]
                               │
               ┌───────────────┼────────────────┐
               ▼                                ▼
      [ n-gram generator ]            [ Tiny Transformer + head training ]
               │                                │
               └──────────────┬─────────────────┘
                              ▼
                    [ Constrained decoding + RAG ]
```

---

## Solution Layout

```
BPEngine.sln
├─ src
│  ├─ BPEngine.Tokenizer      # core BPE + TikToken + presets
│  ├─ BPEngine.Trainer        # train merges + vocab
│  ├─ BPEngine.Transformers   # tiny transformer + head trainer
│  ├─ BPEngine.Generation     # constrained decoding + samplers
│  ├─ BPEngine.RAG            # TF-IDF retriever
│  ├─ BPEngine.Cli            # CLI commands
└─ tests
   └─ BPEngine.Tests
```

---

## Getting Started

### Build
```bash
dotnet build
```

### Encode / Decode (GPT-2 style)
```bash
dotnet run --project ./src/BPEngine.Cli -- encode   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --text "Hello, world!"
```

### Encode (cl100k style)
```bash
dotnet run --project ./src/BPEngine.Cli -- encode   --preset cl100k   --ranks ./data/demo/cl100k_base.json   --text "Hello, world!"
```

### Analyze
```bash
dotnet run --project ./src/BPEngine.Cli -- analyze   --corpus ./data/demo/corpus.txt   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --top 20 --bins 10 --perf
```

### N-Gram Model
```bash
dotnet run --project ./src/BPEngine.Cli -- ngram train   --order 3 --corpus ./data/demo/corpus.txt   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --out ./artifacts/ngram.json

dotnet run --project ./src/BPEngine.Cli -- ngram generate   --order 3 --model ./artifacts/ngram.json   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --prompt "Incident summary:" --max 60
```

### Train Transformer Head
```bash
dotnet run --project ./src/BPEngine.Cli -- train-head   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --corpus ./data/demo/corpus.txt   --out ./artifacts/wout.bin   --steps 500 --batch 8 --seqlen 64

dotnet run --project ./src/BPEngine.Cli -- gen-head   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --wout ./artifacts/wout.bin   --prompt "Draft an incident summary:" --max-new 100
```

### Retrieval (RAG)
```bash
dotnet run --project ./src/BPEngine.Cli -- rag build   --corpus ./data/demo/corpus.txt   --out ./artifacts/tfidf.idx

dotnet run --project ./src/BPEngine.Cli -- rag query   --corpus ./data/demo/corpus.txt   --q "authentication outage"
```

---

## Working with `corpus.txt`

[`data/demo/corpus.txt`](./data/demo/corpus.txt) is the **training dataset**.  
It contains realistic IT/enterprise text (incidents, changes, RCA notes).

- Add more examples to improve results.
- Retrain merges/vocab only when you add lots of new terminology.
- Otherwise just rerun `train-head` to improve learning.

---

## Roadmap

- [x] GPT-2 tokenizer in C#.
- [x] CLI: encode/decode/train/analyze/snippet.
- [x] N-Gram baseline.
- [x] Tiny Transformer playground.
- [x] Trainable head.
- [x] cl100k preset support.
- [x] Constrained JSON decoding.
- [x] TF-IDF retrieval for RAG.
- [ ] Tool calling and schema enforcement.
- [ ] SIMD + Span perf optimizations.
- [ ] NuGet packaging.
- [ ] Extended demo corpora.
- [ ] Docs site + tutorials.

---

## License

MIT (planned). GPT-style BPE originally described by Sennrich et al. (2016) and popularized by OpenAI GPT-2.
