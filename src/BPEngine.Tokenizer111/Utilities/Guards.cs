using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Utilities
{
    public static class Guards
    {
        public static void FileExists(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                throw new TokenizerConfigException($"{name} file not found: {path}");
        }
    }
}
