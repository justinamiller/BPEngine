
# BPEngine.Trainer

A minimal, **all‑C#** byte‑level BPE trainer to produce `merges.txt` and `vocab.json` compatible with the tokenizer.

## What it does
- Reads UTF‑8 text files.
- Pretokenizes with GPT‑style regex (or whitespace).
- Maps tokens into **byte‑level unicode**.
- Iteratively merges the most frequent adjacent pairs until you hit your target size.
- Exports:
  - `merges.txt` — ordered `(left right)` pairs.
  - `vocab.json` — token → id, including specials + base 256 symbols + observed merges.

## Basic usage (programmatic)

```csharp
using BPEngine.Trainer;

var opts = new TrainerOptions
{
    VocabSize = 5000,
    Specials = new Dictionary<string,int> { ["<|bos|>"]=0, ["<|eos|>"]=1 }
};

var trainer = new BpeTrainer(opts);
var (merges, vocab) = trainer.Train(new [] { "corpus.txt" });

TrainerArtifacts.WriteMerges("./out/merges.txt", merges);
TrainerArtifacts.WriteVocab("./out/vocab.json", vocab);
```

## Options
- `VocabSize` — total size including specials (default 5000). If `MaxMerges` is set, it takes precedence.
- `MinPairFrequency` — ignore infrequent pairs (noise).
- `UseGptRegexPretokenizer` — GPT‑like regex vs. whitespace split.

## Notes
- This is a **demo‑grade** trainer intended to bootstrap vocab/merges entirely in C#.
- For large corpora, consider batching and persistent pair counting.
- You can later feed the produced artifacts into `BPEngine.Tokenizer` with `TokenizerFactory.CreateFromFiles(...)`.
