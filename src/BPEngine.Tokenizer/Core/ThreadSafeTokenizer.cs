using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Tokenizer.Core
{
    public sealed class ThreadSafeTokenizer
    {
        private readonly Func<ITokenizer> _factory;
        [ThreadStatic] private static ITokenizer? _threadInst;

        public ThreadSafeTokenizer(Func<ITokenizer> factory) => _factory = factory;
        private ITokenizer Instance => _threadInst ??= _factory();

        public int[] Encode(string text) => Instance.Encode(text);
        public string Decode(IEnumerable<int> ids) => Instance.Decode(ids);
    }
}
