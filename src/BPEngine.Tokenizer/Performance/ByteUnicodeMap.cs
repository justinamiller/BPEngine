using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Performance
{
    /// <summary>
    /// Array-backed mapping for byte<->unicode used by byte-level BPE.
    /// Faster than Dictionary lookups in tight loops.
    /// </summary>
    public sealed class ByteUnicodeMap
    {
        public char[] ByteToChar { get; }
        public byte[] CharToByte; // sparse; only indices for mapped codepoints are filled

        private ByteUnicodeMap(char[] b2c, byte[] c2b)
        {
            ByteToChar = b2c;
            CharToByte = c2b;
        }

        public static ByteUnicodeMap Build()
        {
            // Matches ByteUnicodeMapper logic
            var bs = new System.Collections.Generic.List<int>();
            bs.AddRange(System.Linq.Enumerable.Range(33, 94));   // 33..126
            bs.AddRange(System.Linq.Enumerable.Range(161, 95));  // 161..255

            var cs = new System.Collections.Generic.List<int>(bs);
            int n = 0;
            for (int b = 0; b < 256; b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add(256 + n);
                    n++;
                }
            }

            var b2c = new char[256];
            int maxCode = 0;
            for (int i = 0; i < bs.Count; i++)
            {
                var ch = (char)cs[i];
                b2c[(byte)bs[i]] = ch;
                if (ch > maxCode) maxCode = ch;
            }
            var c2b = new byte[maxCode + 1];
            for (int i = 0; i < bs.Count; i++)
            {
                c2b[(char)cs[i]] = (byte)bs[i];
            }

            return new ByteUnicodeMap(b2c, c2b);
        }
    }
}
