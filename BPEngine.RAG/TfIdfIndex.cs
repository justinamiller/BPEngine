using System.Text.RegularExpressions;

namespace BPEngine.RAG
{
    public sealed partial class TfIdfIndex
    {
        private readonly Dictionary<string, int> _vocab = new();
        private readonly List<Dictionary<int, double>> _docs = new(); // sparse tf
        private double[]? _idf;

        static readonly Regex TokenRx = new(@"[A-Za-z0-9_]+", RegexOptions.Compiled);

        public int AddDocument(string text)
        {
            var tf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TokenRx.Matches(text))
            {
                var t = m.Value;
                tf[t] = tf.TryGetValue(t, out var c) ? c + 1 : 1;
            }

            var row = new Dictionary<int, double>();
            foreach (var (tok, cnt) in tf)
            {
                if (!_vocab.TryGetValue(tok, out var idx)) { idx = _vocab.Count; _vocab[tok] = idx; }
                row[idx] = cnt;
            }
            _docs.Add(row);
            _idf = null; // invalidate until Fit()
            return _docs.Count - 1;
        }

        public void Fit()
        {
            int V = _vocab.Count; int N = _docs.Count;
            var df = new int[V];
            foreach (var d in _docs)
                foreach (var idx in d.Keys) df[idx]++;

            _idf = new double[V];
            for (int i = 0; i < V; i++) _idf[i] = Math.Log((N + 1.0) / (df[i] + 1.0)) + 1.0;
        }

        public (int Doc, double Score)[] Query(string text, int k = 5)
        {
            if (_idf is null) Fit();
            var qtf = new Dictionary<int, double>();
            foreach (Match m in TokenRx.Matches(text))
            {
                if (_vocab.TryGetValue(m.Value, out var idx))
                    qtf[idx] = qtf.TryGetValue(idx, out var c) ? c + 1 : 1;
            }
            // cosine scores
            var scores = new List<(int, double)>();
            var (qnorm, qvec) = NormWeighted(qtf, _idf!);
            for (int d = 0; d < _docs.Count; d++)
            {
                var (dnorm, dvec) = NormWeighted(_docs[d], _idf!);
                double dot = 0;
                foreach (var (idx, qv) in qvec) if (dvec.TryGetValue(idx, out var dv)) dot += qv * dv;
                var sim = (qnorm == 0 || dnorm == 0) ? 0 : dot / (qnorm * dnorm);
                scores.Add((d, sim));
            }
            return scores.OrderByDescending(s => s.Item2).Take(k).ToArray();
        }

        private static (double, Dictionary<int, double>) NormWeighted(Dictionary<int, double> tf, double[] idf)
        {
            var w = new Dictionary<int, double>(tf.Count);
            double sum2 = 0;
            foreach (var (idx, v) in tf)
            {
                var val = (1 + Math.Log(v)) * idf[idx];
                w[idx] = val; sum2 += val * val;
            }
            return (Math.Sqrt(sum2), w);
        }
    }
}
