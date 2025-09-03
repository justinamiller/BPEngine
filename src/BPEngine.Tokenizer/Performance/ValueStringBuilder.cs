using System;
using System.Text;

namespace BPEngine.Tokenizer.Performance
{
    /// <summary>
    /// Lightweight, stackalloc-friendly string builder.
    /// Wraps a Span&lt;char&gt; and only allocates on the heap if capacity is exceeded.
    /// Useful for hot paths like merge joining where <see cref="StringBuilder"/> is too heavy.
    /// </summary>
    internal ref struct ValueStringBuilder
    {
        private Span<char> _buffer;
        private char[]? _pooled; // backing array if we exceed stackalloc size
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _buffer = initialBuffer;
            _pooled = null;
            _pos = 0;
        }

        /// <summary>
        /// Current number of characters written.
        /// </summary>
        public int Length => _pos;

        /// <summary>
        /// Returns the built string and clears the builder.
        /// </summary>
        public override string ToString()
        {
            var s = new string(_buffer[.._pos]);
            Dispose();
            return s;
        }

        /// <summary>
        /// Append a single character.
        /// </summary>
        public void Append(char c)
        {
            if (_pos < _buffer.Length)
            {
                _buffer[_pos++] = c;
            }
            else
            {
                Grow(1);
                _buffer[_pos++] = c;
            }
        }

        /// <summary>
        /// Append a slice of characters.
        /// </summary>
        public void Append(ReadOnlySpan<char> span)
        {
            if (_pos + span.Length <= _buffer.Length)
            {
                span.CopyTo(_buffer[_pos..]);
                _pos += span.Length;
            }
            else
            {
                Grow(span.Length);
                span.CopyTo(_buffer[_pos..]);
                _pos += span.Length;
            }
        }

        private void Grow(int needed)
        {
            int newSize = Math.Max(_pos + needed, _buffer.Length * 2);
            char[] newArray = new char[newSize];
            _buffer[.._pos].CopyTo(newArray);
            _buffer = newArray;
            _pooled = newArray;
        }

        /// <summary>
        /// Dispose releases the pooled array reference.
        /// </summary>
        public void Dispose()
        {
            _pos = 0;
            _buffer = Span<char>.Empty;
            _pooled = null;
        }
    }
}
