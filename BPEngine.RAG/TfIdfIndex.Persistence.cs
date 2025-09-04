using System.Text;

namespace BPEngine.RAG
{
    public sealed partial class TfIdfIndex
    {
        // Make the core class 'partial' or move these into the same file.
        public void Save(string path)
        {
            using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false);

            bw.Write(1); // version

            // vocab
            bw.Write(_vocab.Count);
            foreach (var kv in _vocab)
            {
                bw.Write(kv.Key);
                bw.Write(kv.Value);
            }

            // idf
            var idf = _idf ?? Array.Empty<double>();
            bw.Write(idf.Length);
            for (int i = 0; i < idf.Length; i++) bw.Write(idf[i]);

            // docs
            bw.Write(_docs.Count);
            foreach (var row in _docs)
            {
                bw.Write(row.Count);
                foreach (var kv in row)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value);
                }
            }
        }

        public static TfIdfIndex Load(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            var ver = br.ReadInt32();
            if (ver != 1) throw new InvalidOperationException($"Unsupported TF-IDF index version {ver}");

            var vocab = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var vcount = br.ReadInt32();
            for (int i = 0; i < vcount; i++)
            {
                var tok = br.ReadString();
                var idx = br.ReadInt32();
                vocab[tok] = idx;
            }

            var idfLen = br.ReadInt32();
            var idf = new double[idfLen];
            for (int i = 0; i < idfLen; i++) idf[i] = br.ReadDouble();

            var docCount = br.ReadInt32();
            var docs = new List<Dictionary<int, double>>(docCount);
            for (int d = 0; d < docCount; d++)
            {
                var n = br.ReadInt32();
                var row = new Dictionary<int, double>(n);
                for (int j = 0; j < n; j++)
                {
                    var k = br.ReadInt32();
                    var v = br.ReadDouble();
                    row[k] = v;
                }
                docs.Add(row);
            }

            var idxObj = new TfIdfIndex();
            // assign private fields via reflection or friend – or add internal setters:
            idxObj._vocab.Clear();
            foreach (var kv in vocab) idxObj._vocab[kv.Key] = kv.Value;
            idxObj._docs.Clear();
            idxObj._docs.AddRange(docs);
            idxObj._idf = idf;
            return idxObj;
        }
    }
}
