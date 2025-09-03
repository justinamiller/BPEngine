using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Performance
{
    internal static class SimdHelpers
    {
        /// <summary>
        /// Returns true if all chars in the span are ASCII (<= 0x7F).
        /// Useful for fast-paths in pre-tokenization or mapping.
        /// </summary>
        public static bool IsAscii(ReadOnlySpan<char> s)
        {
#if NET8_0_OR_GREATER
            if (Vector.IsHardwareAccelerated && s.Length >= Vector<ushort>.Count)
            {
                var vecCount = Vector<ushort>.Count;
                int i = 0;
                var mask = new Vector<ushort>(0xFF80); // checks bits above 0x7F
                while (i <= s.Length - vecCount)
                {
                    var v = new Vector<ushort>(MemoryMarshal.Cast<char, ushort>(s.Slice(i, vecCount)));
                    if (!Vector.EqualsAll(v & mask, Vector<ushort>.Zero))
                        return false;
                    i += vecCount;
                }
                for (; i < s.Length; i++)
                    if (s[i] > 0x7F) return false;
                return true;
            }
#endif
            for (int i = 0; i < s.Length; i++)
                if (s[i] > 0x7F) return false;
            return true;
        }

        /// <summary>
        /// Counts occurrences of a specific char using SIMD where available.
        /// </summary>
        public static int CountOf(ReadOnlySpan<char> s, char ch)
        {
#if NET8_0_OR_GREATER
            if (Vector.IsHardwareAccelerated && s.Length >= Vector<ushort>.Count)
            {
                var vecCount = Vector<ushort>.Count;
                var target = new Vector<ushort>(ch);
                int i = 0, count = 0;
                while (i <= s.Length - vecCount)
                {
                    var v = new Vector<ushort>(MemoryMarshal.Cast<char, ushort>(s.Slice(i, vecCount)));
                    var eq = Vector.Equals(v, target);
                    for (int j = 0; j < vecCount; j++)
                        if (eq[j] == ushort.MaxValue) count++;
                    i += vecCount;
                }
                for (; i < s.Length; i++) if (s[i] == ch) count++;
                return count;
            }
#endif
            int c = 0;
            for (int i = 0; i < s.Length; i++) if (s[i] == ch) c++;
            return c;
        }
    }
}
