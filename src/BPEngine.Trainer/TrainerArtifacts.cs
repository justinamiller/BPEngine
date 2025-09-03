
using System.Text;
using System.Text.Json;

namespace BPEngine.Trainer
{
    public static class TrainerArtifacts
    {
        public static void WriteMerges(string path, IEnumerable<(string Left, string Right)> merges)
        {
            using var sw = new StreamWriter(path, false, Encoding.UTF8);
            sw.WriteLine("#version: 0.2");
            foreach (var (l, r) in merges)
                sw.WriteLine($"{l} {r}");
        }

        public static void WriteVocab(string path, Dictionary<string,int> vocab)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(vocab, opts), Encoding.UTF8);
        }
    }
}
