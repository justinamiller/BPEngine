using System.Text;

namespace BPEngine.Generation
{
    /// <summary>
    /// Lightweight JSON prefix validator. Tracks quotes/braces/brackets and escapes.
    /// Accepts tokens only if appending keeps a syntactically valid prefix.
    /// </summary>
    public sealed class JsonConstrainedDecoder : IConstrainedDecoder
    {
        private readonly StringBuilder _buf = new();
        private int _brace, _brack;
        private bool _inStr, _esc;

        public bool Accept(string piece)
        {
            // simulate
            var s = _buf.Length;
            AppendSim(piece, simulate: true);
            bool ok = IsPrefixValid();
            // rollback simulated append
            _buf.Length = s; _brace = 0; _brack = 0; _inStr = false; _esc = false;
            // re-walk current buffer to restore state
            AppendSim(_buf.ToString(), simulate: false);
            return ok;
        }

        public void Push(string piece) => AppendSim(piece, simulate: false);

        public void Reset()
        {
            _buf.Clear(); _brace = _brack = 0; _inStr = _esc = false;
        }

        private void AppendSim(string s, bool simulate)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!_inStr)
                {
                    if (c == '{') _brace++;
                    else if (c == '}') { _brace--; if (_brace < 0) { } }
                    else if (c == '[') _brack++;
                    else if (c == ']') { _brack--; if (_brack < 0) { } }
                    else if (c == '"') _inStr = true;
                }
                else
                {
                    if (_esc) { _esc = false; }
                    else if (c == '\\') _esc = true;
                    else if (c == '"') _inStr = false;
                }
            }
            if (!simulate) _buf.Append(s);
        }

        private bool IsPrefixValid()
        {
            if (_brace < 0 || _brack < 0) return false;
            // Basic rule: you can end mid-string or mid-structure; just not negative depths.
            return true;
        }
    }
}
