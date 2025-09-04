# Demo: Enterprise Incident & Change Management (CPU-only, pure C#)

## Files
- ./data/demo/BIG_gpt2_merges.txt
- ./data/demo/BIG_vocab.json
- ./data/demo/corpus.txt

## 0) Build
```bash
dotnet build
```

## 1) Encode/Decode (domain tokens)
```bash
dotnet run --project ./src/BPEngine.Cli -- encode   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --text "P1 incident: token service outage; rollback and restore cache."
```

## 2) Token Budgeting (prep prompts for bots/LLMs)
```bash
dotnet run --project ./src/BPEngine.Cli -- snippet   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --text "$(cat ./data/demo/corpus.txt)"   --budget 256
```

## 3) Analyze corpus (data profiling)
```bash
dotnet run --project ./src/BPEngine.Cli -- analyze   --corpus ./data/demo/corpus.txt   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --top 20 --bins 10 --perf
```

## 4) Train tokenizer on the corpus (domain compression)
```bash
dotnet run --project ./src/BPEngine.Cli -- train   --corpus ./data/demo/corpus.txt   --out ./artifacts   --vocab-size 1200   --min-pair 2
```

## 5) Lightweight generation (n-gram baseline)
```bash
dotnet run --project ./src/BPEngine.Cli -- ngram train   --order 3   --corpus ./data/demo/corpus.txt   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --out ./artifacts/ngram_incident.json

dotnet run --project ./src/BPEngine.Cli -- ngram generate   --order 3   --model ./artifacts/ngram_incident.json   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --prompt "Summary: Authentication outage"   --max 80 --temp 0.9 --topk 40
```

## 6) Trainable head (it actually learns on CPU, no libs)
```bash
dotnet run --project ./src/BPEngine.Cli -- train-head   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --corpus ./data/demo/corpus.txt   --out    ./artifacts/wout_incident.bin   --dim 64 --heads 2 --layers 2 --max-seq 64   --batch 8 --steps 400 --seqlen 64 --lr 0.01

dotnet run --project ./src/BPEngine.Cli -- gen-head   --merges ./data/demo/BIG_gpt2_merges.txt   --vocab  ./data/demo/BIG_vocab.json   --wout   ./artifacts/wout_incident.bin   --prompt "Draft a short post-incident summary:"   --max-new 100 --temp 0.9 --topk 40
```
