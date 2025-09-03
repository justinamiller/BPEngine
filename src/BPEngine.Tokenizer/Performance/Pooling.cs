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
    }//end poolarray

}
