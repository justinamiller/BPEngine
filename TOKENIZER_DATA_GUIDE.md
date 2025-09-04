# Guide: Creating Tokenizer Data Files

BPEngine relies on three main data artifacts:

- **`corpus.txt`** → raw training examples  
- **`BIG_gpt2_merges.txt`** → merge rules (BPE steps)  
- **`BIG_vocab.json`** → mapping from token string → integer ID  

Together, these define how text is split into tokens and converted into IDs.

---

## 1. `corpus.txt` — The Training Data

This is just **plain text**. Each line or block is an example from your domain.

### How to create it
1. Collect real or synthetic examples of text you want the tokenizer to understand.
   - Incident tickets
   - Change requests
   - RCA summaries
   - Architecture notes
   - Logs, error codes, identifiers
   - Customer-facing communications
2. Save them as plain UTF-8 text in `data/demo/corpus.txt`.

### Example
Incident: INC-2025-00421
Severity: P1
Summary: Authentication outage in US region impacted user login.
Impact: 37% of login attempts failed with 401/403 errors.
Root Cause: Token service deploy introduced cache key mismatch.
Resolution: Rollback deployment, purge stale keys, restore cache.
Next Steps: Add canary checks, expand dashboard alerts, raise SLO budgets.


### How to expand
- Keep adding more examples over time — more rows means better training.
- You don’t need to change merges/vocab every time; just rerun `train-head` to make the model learn from new text.
- Only retrain merges/vocab if your new corpus introduces lots of new terminology (see sections below).

---

## 2. `BIG_gpt2_merges.txt` — The Merge Rules

This file defines the **byte-pair encoding steps** (what pairs of characters/subwords get merged).

### How to create it
You don’t hand-edit this. You generate it using the BPEngine CLI:

```bash
dotnet run --project ./src/BPEngine.Cli -- train \
  --corpus ./data/demo/corpus.txt \
  --out ./artifacts \
  --vocab-size 32000 \
  --min-pair 2

  This produces:

./artifacts/merges.txt
./artifacts/vocab.json

cp ./artifacts/merges.txt ./data/demo/BIG_gpt2_merges.txt
cp ./artifacts/vocab.json ./data/demo/BIG_vocab.json

#version: 0.2 is always the header line.

Each following line is a merge rule, e.g.:
Ġ t
Ġ th
Ġ the


3. BIG_vocab.json — The Vocabulary

This is a JSON dictionary mapping each token string to a numeric ID.

How to create it

It is generated together with merges.txt when you run the train command above.

Example snippet:
{
  "Ġthe": 4,
  "Ġincident": 15,
  "Ġerror": 27,
  "Ġsla": 92,
  "INC-": 1450,
  "2025": 1451,
  "<|bos|>": 32000,
  "<|eos|>": 32001,
  "<|pad|>": 32002
}

Notes

Special tokens like <|bos|>, <|eos|>, <|pad|> are usually added automatically or can be injected manually.

IDs must be unique and stable for a given vocab file.


4. Best Practices

Corpus First
Always start from a large, representative corpus.txt. These examples drive merges and vocab quality.

Evergreen Seed File
Optionally create a SeedCorpus_Enterprise.txt with critical tokens (SKUs, region names, acronyms, HTTP codes) repeated many times. Concatenate it with your real corpus before training. This ensures those terms get their own tokens.

Versioning
Keep dated/numbered versions:
BIG_gpt2_merges.v1.txt
BIG_vocab.v1.json

When you retrain, bump the version.

Stability
Don’t regenerate merges/vocab lightly. If your IDs change, any trained head weights won’t align. Only retrain when:

entering a new domain with lots of new terms, or

you want to deliberately expand vocab size.