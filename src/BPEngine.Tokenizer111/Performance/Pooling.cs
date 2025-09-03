using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Performance
{
    /// <summary>
    /// Disposable wrapper around ArrayPool<T>.Rent/Return with Span access.
    /// Use with 'using var' to ensure Return() is called.
    /// </summary>
    internal sealed class PooledArray<T> : IDisposable
    {
        private T[] _array;
        private readonly ArrayPool<T> _pool;
        private bool _disposed;

        public PooledArray(int minimumLength, ArrayPool<T>? pool = null)
        {
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = _pool.Rent(minimumLength);
        }

        public Span<T> Span => _array.AsSpan();
        public Memory<T> Memory => _array.AsMemory();
        public T[] Array => _array;

        public void Dispose()
        {
            if (_disposed) return;
            _pool.Return(_array, clearArray: RuntimeHelpersIsReferenceOrContainsReferences<T>());
            _disposed = true;
        }

        private static bool RuntimeHelpersIsReferenceOrContainsReferences<TType>()
        {
#if NET8_0_OR_GREATER
            return System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<TType>();
#else
            // Conservative: clear array for ref-like types on older TFMs
            return true;
#endif
        }
    }

    /// <summary>
    /// A lightweight string builder that uses stackalloc for small sizes
    /// and ArrayPool<char> for larger buffers. NOT thread-safe.
    /// </summary>
    public ref struct ValueStringBuilder
    {
        private Span<char> _span;
        private char[]? _arrayFromPool;
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _span = initialBuffer;
            _arrayFromPool = null;
            _pos = 0;
        }

        public int Length => _pos;
        public ReadOnlySpan<char> AsSpan() => _span.Slice(0, _pos);

        public void Append(char c)
        {
            if ((uint)_pos < (uint)_span.Length)
            {
                _span[_pos++] = c;
            }
            else
            {
                Grow(1);
                _span[_pos++] = c;
            }
        }

        public void Append(ReadOnlySpan<char> s)
        {
            if (s.Length == 0) return;
            var required = _pos + s.Length;
            if (required > _span.Length) Grow(s.Length);
            s.CopyTo(_span.Slice(_pos));
            _pos += s.Length;
        }

        public override string ToString()
        {
            var s = _span.Slice(0, _pos).ToString();
            Dispose();
            return s;
        }

        public void Dispose()
        {
            var toReturn = _arrayFromPool;
            this = default;
            if (toReturn is not null)
                ArrayPool<char>.Shared.Return(toReturn);
        }

        private void Grow(int additionalChars)
        {
            int newSize = Math.Max(_pos + additionalChars, _span.Length * 2);
            char[] poolArray = ArrayPool<char>.Shared.Rent(newSize);
            _span.Slice(0, _pos).CopyTo(poolArray);
            _span = poolArray;
            _arrayFromPool = poolArray;
        }
    }
}
