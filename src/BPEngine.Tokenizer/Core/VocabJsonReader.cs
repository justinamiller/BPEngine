using BPEngine.Tokenizer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public class VocabJsonReader
    {
        public static Dictionary<string, int> Load(string path)
        {
            Guards.FileExists(path, "vocab.json");
            using var fs = File.OpenRead(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(fs);
            if (dict is null || dict.Count == 0)
                throw new VocabNotFoundException($"Empty or invalid vocab.json at {path}");
            return dict;
        }
    }
}
