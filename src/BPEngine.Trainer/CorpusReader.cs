
using System.Collections.Concurrent;
using System.Text;
using BPEngine.Tokenizer; // for RegexPreTokenizer and ByteUnicodeMapper

namespace BPEngine.Trainer
{
    public static class CorpusReader
    {
        /// <summary>
        /// Streams pretokenized strings from one or more UTF‑8 text files.
        /// </summary>
        public static IEnumerable<string> StreamTokens(IEnumerable<string> paths, bool useGptRegex)
        {
            foreach (var path in paths)
            {
                using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string? line;
                while ((line = sr.ReadLine()) is not null)
                {
                    if (useGptRegex)
                    {
                        foreach (var tok in BPEngine.Tokenizer.RegexPreTokenizer.Split(line))
                            if (tok.Length > 0) yield return tok;
                    }
                    else
                    {
                        foreach (var tok in line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries))
                            yield return tok;
                    }
                }
            }
        }
    }
}
