# BPEngine – Tokenizer & Transformer Playground (C#)

**BPEngine** is a complete tokenizer and mini-model playground written fully in **C#**.  
It provides the full lifecycle of a GPT-style Byte-Pair Encoding (BPE) tokenizer, plus simple statistical and neural model demos.

---

## Features

### Tokenizer (production-ready)
- GPT-2/3 style Byte-Pair Encoding (BPE) tokenizer
- Full support for:
  - Encode text → token IDs
  - Decode token IDs → text
  - Special tokens (e.g. `<|bos|>`, `<|eos|>`)
- Optimized with LRU caching for performance
- Configurable via `TokenizerOptions`

### Trainer
- Train merges + vocab from your own corpus
- Output: `merges.txt` + `vocab.json` artifacts
- Deterministic training on small samples

### Analyzer
- Compute tokenization statistics over corpora:
  - Token length distributions
  - Histograms
  - Top tokens & bigrams
- Output to human-readable or JSON

### Models
- N-gram model for simple sequence generation
- Learn from corpus, then generate token sequences

### Tiny Transformer Playground
- A miniature GPT-like model implemented in C#
- Educational forward-pass only:
  - Embeddings
  - Positional encoding
  - Multi-head self-attention
  - Feed-forward layers
- Demo command to generate text with random weights
- Great for learning, debugging, and experimentation

### CLI
Unified tool for all functions:
```bash
bpe encode     --merges merges.txt --vocab vocab.json --text "Hello world"
bpe decode     --merges merges.txt --vocab vocab.json --ids 15496,995
bpe train      --corpus data.txt --out ./artifacts
bpe analyze    --corpus data.txt --merges merges.txt --vocab vocab.json
bpe ngram train/generate ...
bpe transformer demo --merges merges.txt --vocab vocab.json --prompt "Hello"
