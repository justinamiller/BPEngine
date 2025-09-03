# BPEngine

BPEngine is a **byte‑pair encoding (BPE)** tokenizer toolkit written in **C#**—in the style used by the GPT‑2/3 family.  
The long‑term goal is to provide a **100% .NET** implementation that supports the **full lifecycle** of a language‑model
text pipeline: **train → package → serve → integrate**, with no Python runtime required.

> If you want to kick the tires right now, start with the `ByteLevelBPETokenizer` library and the demo merges/vocab
> (compatible with GPT‑style byte‑level BPE).

---

## Vision & Scope

**Why BPEngine?**  
Most open implementations rely on Python for tokenization, training, or runtime glue. BPEngine aims to be a **first‑class, production‑grade C#** alternative for teams that standardize on .NET.

**Target outcomes:**

- **Tokenizer parity** with GPT‑2/3 byte‑level BPE (regex pre‑tokenizer, bytes↔unicode mapping, merges application).
- **Zero Python dependency** across the whole lifecycle.
- **Ergonomic APIs** for apps, services, and data pipelines.
- **Deterministic, testable, and high‑performance** behavior suitable for enterprise workloads.

---

## Architecture (Full Lifecycle in C#)

```
[ Data Corpus ] 
      │
      ▼
[ BPE Trainer ]  ──►  merges.txt + vocab.json   ─┐
                                                 │
                                          [ Packaging ]
                                                 │
                                                 ▼
                                       [ BPEngine.Tokenizer ]
                                                 │
                       ┌─────────────────────────┴─────────────────────────┐
                       ▼                                                   ▼
              [ Inference Runtime ]                                [ Tooling / CLI ]
                       │                                                   │
                       ▼                                                   ▼
                 .NET apps / APIs                                   DevOps & CI/CD
```

**Modules (planned):**
- **`BPEngine.Tokenizer`** – Byte‑level BPE encode/decode (done first).
- **`BPEngine.Trainer`** – Train BPE merges/vocab from a corpus (C# only).
- **`BPEngine.Runtime`** – Utilities for serving tokenization at scale (pools, SIMD, caching).
- **`BPEngine.Cli`** – Command‑line tools for encode/decode, train, and validate.
- **`BPEngine.Tests`** – Unit/benchmark tests for determinism and performance.

---

## Key Features

- **Byte‑Level BPE** compatible approach (GPT‑2/3 style):
  - Regex pre‑tokenization close to GPT‑2.
  - Bytes→Unicode trick to ensure lossless round‑trip.
  - Merge‑rank application with caching for speed.
- **Special tokens** support (e.g., `<|bos|>`, `<|eos|>`, `<|pad|>`).
- **Pluggable vocab/merges** (`vocab.json`, `merges.txt`).
- **Deterministic decode** with explicit ID→token maps.
- **High‑performance C#** implementation; no Python interop.

---

## Getting Started

### 1) Install / Reference
Add the Tokenizer project to your solution (or reference the DLL/NuGet once published).

```
dotnet add package BPEngine.Tokenizer   # (planned)
```

### 2) Files
Use a GPT‑style **merges file** and (optionally) a **vocab.json**:

- `gpt2_merges.txt` (subset for testing): download from your project’s artifacts or the provided demo.
- `vocab.json` (optional for fixed IDs). Without it, IDs can be assigned dynamically for local use.

> Demo files:
> - `/mnt/data/gpt2_merges_demo/gpt2_merges.txt`
> - `/mnt/data/gpt2_merges_demo/vocab.json`

### 3) Sample Code

```csharp
var mergesPath = "gpt2_merges.txt";

// Optional fixed IDs for specials (recommended for consistency)
var specials = new Dictionary<string, int>
{
    ["<|bos|>"] = 0,
    ["<|eos|>"] = 1,
    ["<|pad|>"] = 2
};

// Load vocab.json if you want stable IDs for all tokens
Dictionary<string,int>? vocab = null;
// vocab = LoadVocab("vocab.json"); // implement a simple JSON loader

var tok = new ByteLevelBPETokenizer(mergesPath, tokenToId: vocab, specialTokenToId: specials);

// Encode
var ids = tok.Encode("<|bos|>Hello, world!<|eos|>");
Console.WriteLine(string.Join(", ", ids));

// Decode
var text = tok.Decode(ids);
Console.WriteLine(text);
```

---

## Design Notes

### Byte‑Level BPE
- Tokenization happens **after** mapping UTF‑8 bytes into a stable unicode range to preserve all byte values.
- Merges are applied using a **rank table** derived from `merges.txt` (lower rank = higher priority).
- A small **LRU cache** (or dictionary cache) dramatically reduces repeated work on common substrings.

### Regex Pre‑Tokenizer
- GPT‑style regex segments text into:
  - words (letters/numbers), contractions, punctuation, and whitespace blocks.
- Small regex differences **change token boundaries**; we follow GPT‑2 conventions closely.

### IDs & Vocab
- With `vocab.json`, token strings map to **stable IDs** (required for model compatibility).
- Without it, BPEngine can **assign IDs on the fly**—useful for local apps but not model interop.

---

## Solution Layout (suggested)

```
BPEngine.sln
├─ src
│  ├─ BPEngine.Tokenizer
│  │  ├─ ByteLevelBPETokenizer.cs
│  │  └─ (helpers: Regex, Bytes↔Unicode, Cache)
│  ├─ BPEngine.Trainer            # (planned)
│  ├─ BPEngine.Runtime            # (planned)
│  └─ BPEngine.Cli                # (planned)
└─ tests
   └─ BPEngine.Tests              # NB: determinism & round‑trip tests
```

---

## Roadmap

- [x] Byte‑level BPE encode/decode library (MVP).
- [ ] **Trainer**: learn merges/vocab from a corpus (C# only).
- [ ] **Faster paths**: SIMD/intrinsics, pooling, and Span‑based parsing.
- [ ] **CLI**: `bpe encode/ decode/ train/ validate` commands.
- [ ] **ONNX/Interop tools** for exporting tokenizers.
- [ ] **NuGet** release and documentation site.
- [ ] **Benchmarks** against reference implementations (size and speed).

---

## Performance & Testing

- **Determinism tests**: identical input → identical IDs; decode(encode(x)) == x.
- **Compatibility tests**: compare against known GPT‑2 token splits for a fixture set.
- **Performance**: target low allocations and O(pairs) merging with caching.

---

## File Formats

- **`merges.txt`**: ordered list of space‑separated token pairs; earlier lines have **higher priority**.
- **`vocab.json`**: JSON map of `token -> id`. For decode, keep a reverse map `id -> token`.

Example `merges.txt` line:
```
t h
th e
in g
```

Example `vocab.json` entry:
```json
{ "the": 1234 }
```

---

## Security & Safety

- Tokenizers process untrusted input. Ensure:
  - Bounded memory usage on large payloads.
  - Timeouts or circuit breakers in service contexts.
  - Validation on file loads (merges/vocab).

---

## License & Attribution

- BPEngine will include a permissive license (e.g., MIT) unless otherwise specified.
- GPT‑style BPE concepts originate from Sennrich et al. (2016) and OpenAI’s GPT‑2 tokenizer design.

---

## Contributing

- Open to PRs for: training algorithms, SIMD optimizations, Windows/Linux CI, and interop helpers.
- Please include **unit tests** and **benchmarks** for any behavior changes.

---

## Contacts / Support

- Issues and requests: GitHub Issues (planned).

---
