namespace BPEngine.Transformers
{
    public static class HeadIo
    {
        public static void SaveWout(TinyTransformer model, string path)
        {
            using var bw = new BinaryWriter(File.Open(path, FileMode.Create));
            bw.Write(model.Dim);
            bw.Write(model.VocabSize);
            foreach (var w in model.Wout) bw.Write(w);
        }

        public static void LoadWout(TinyTransformer model, string path)
        {
            using var br = new BinaryReader(File.OpenRead(path));
            int d = br.ReadInt32();
            int v = br.ReadInt32();
            if (d != model.Dim || v != model.VocabSize)
                throw new InvalidOperationException($"Wout shape mismatch: file=({d},{v}) model=({model.Dim},{model.VocabSize})");
            for (int i = 0; i < model.Wout.Length; i++) model.Wout[i] = br.ReadSingle();
        }
    }
}
