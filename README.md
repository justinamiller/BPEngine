# BPEngine

BPEngine is a **pure C#** implementation of GPT-style **byte-pair encoding (BPE) tokenization** and a playground for lightweight generative models. It is designed to run **end-to-end in .NET**, CPU-only, with **no Python dependencies**.  

The long-term goal: a self-contained .NET solution for the **full lifecycle of text processing**:  
**train → analyze → package → serve → integrate**.

---

## Why BPEngine?

Most open-source tokenizers and toy GPT demos rely on Python. BPEngine is for teams that standardize on .NET and want:

- **Tokenizer parity** with GPT-2/3 byte-level BPE.  
- **Zero Python** across training, runtime, and tooling.  
- **Deterministic + testable** behavior for enterprise workloads.  
- **Educational + practical** modeling tools that run on CPU.  

---

## Features

### Tokenization
- GPT-style **byte-level BPE** (encode/decode).
- **Regex pre-tokenizer** close to GPT-2.
- **Stable vocab/merges** from `merges.txt` + `vocab.json`.
- **Special tokens** (`<|bos|>`, `<|eos|>`, `<|pad|>`).
- **Snippet budgeting**: trim long inputs to a token budget.
- **Performance flags**: tokens/sec, JSON metrics.

### Training
- Train new vocab + merges from any corpus (`train`).
- Seed corpus trick for domain tokens (SKUs, error codes, acronyms).
- Corpus analysis (`analyze`) for token histograms, bigrams, distributions.

### Lightweight Models
- **N-Gram model**: simple baseline generator.  
- **Tiny Transformer demo**: forward-only GPT-like playground.  
- **Trainable Transformer Head**: update the output weights on your own corpus → real learning on CPU.  

---

## Architecture

```
[ corpus.txt ] 
      │
      ▼
[ BPE Trainer ]  ──►  merges.txt + vocab.json   ─┐
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
```

---

## Solution Layout

```
BPEngine.sln
├─ src
│  ├─ BPEngine.Tokenizer      # core BPE encode/decode
│  ├─ BPEngine.Trainer        # train merges + vocab
│  ├─ BPEngine.Transformers   # tiny transformer + head trainer
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

### Encode / Decode
```bash
dotnet run --project ./src/BPEngine.Cli -- encode   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --text "Hello, world!"

dotnet run --project ./src/BPEngine.Cli -- decode   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --ids 15496,995
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

---

## Working with `corpus.txt`

[`data/demo/corpus.txt`](./data/demo/corpus.txt) is the **training dataset**.  
It contains realistic IT/enterprise text (incidents, changes, RCA notes).  

- Add more examples to improve results.  
- Retrain merges/vocab only when you add lots of new terminology.  
- Otherwise just rerun `train-head` to improve learning.  

---

## Roadmap

- [x] GPT-style tokenizer in pure C#.  
- [x] CLI: encode/decode/train/analyze/snippet.  
- [x] N-Gram baseline.  
- [x] Tiny Transformer playground.  
- [x] Trainable head for domain learning.  
- [ ] SIMD + Span perf optimizations.  
- [ ] NuGet packaging.  
- [ ] Extended demo corpora.  
- [ ] Docs site + tutorials.  

---

## License

MIT (planned). GPT-style BPE originally described by Sennrich et al. (2016) and popularized by OpenAI GPT-2.
