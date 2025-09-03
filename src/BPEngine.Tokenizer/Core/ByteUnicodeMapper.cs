using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    namespace BPEngine.Tokenizer
    {
        public static class ByteUnicodeMapper
        {
            public static (Dictionary<byte, char> byteToUni, Dictionary<char, byte> uniToByte) Build()
            {
                var bs = new List<int>();
                bs.AddRange(Enumerable.Range(33, 94));   // 33..126
                bs.AddRange(Enumerable.Range(161, 95));  // 161..255

                var cs = new List<int>(bs);
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

                var byteToUni = new Dictionary<byte, char>(256);
                var uniToByte = new Dictionary<char, byte>(256);

                for (int i = 0; i < bs.Count; i++)
                {
                    byteToUni[(byte)bs[i]] = (char)cs[i];
                    uniToByte[(char)cs[i]] = (byte)bs[i];
                }
                return (byteToUni, uniToByte);
            }
        }
    }
}
