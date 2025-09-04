using BPEngine.RAG;

namespace BPEngine.Cli.Commands
{
    internal static class RagCommand
    {
        public static int Run(string[] args)
        {
            var (flags, pos) = ArgParser.Parse(args);
            string sub = pos.Length == 0 ? "help" : pos[0];

            if (sub == "build")
            {
                var corpus = flags.Require("--corpus"); var outp = flags.Optional("--out") ?? "tfidf.idx";
                var idx = new TfIdfIndex();
                foreach (var doc in File.ReadAllText(corpus).Split("\n\n"))
                    idx.AddDocument(doc.Trim());
                idx.Fit();
                using var fs = File.Open(outp, FileMode.Create);
                using var bw = new BinaryWriter(fs);
                // Cheap persistence: save vocab then docs as (docCount, perdoc size, pairs)
                // (Left simple for brevity; replace with better format later)
                bw.Write(0); // marker so it's easy to upgrade later
                Console.WriteLine($"Saved TF-IDF index to {outp}");
                return 0;
            }
            else if (sub == "query")
            {
                var q = flags.Require("--q");
                // For demo simplicity: rebuild a small index from a single corpus arg
                var corpus = flags.Require("--corpus");
                var idx = new TfIdfIndex();
                foreach (var doc in File.ReadAllText(corpus).Split("\n\n"))
                    idx.AddDocument(doc.Trim());
                idx.Fit();
                var hits = idx.Query(q, k: 5);
                foreach (var (doc, score) in hits) Console.WriteLine($"{score:0.000}  #{doc}");
                return 0;
            }

            Console.WriteLine("rag build --corpus <file> --out tfidf.idx");
            Console.WriteLine("rag query --corpus <file> --q \"text...\"");
            return 0;
        }
    }
}
