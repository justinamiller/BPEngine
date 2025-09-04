using System;
using System.Runtime.InteropServices;

namespace BPEngine.Transformers.Mathx
{
    internal static class TinyMath
    {
        public static void AddInPlace(Span<float> a, ReadOnlySpan<float> b)
        {
            for (int i = 0; i < a.Length; i++) a[i] += b[i];
        }

        public static void MulAddMatVec(float[] W, float[] x, float[] y, int rows, int cols)
        {
            // y = y + W * x
            for (int r = 0; r < rows; r++)
            {
                float acc = 0f;
                int off = r * cols;
                for (int c = 0; c < cols; c++) acc += W[off + c] * x[c];
                y[r] += acc;
            }
        }

        public static void MatMul(float[] A, float[] B, float[] C, int m, int k, int n)
        {
            // C[m,n] = A[m,k] x B[k,n]
            Array.Clear(C, 0, C.Length);
            for (int i = 0; i < m; i++)
            {
                for (int p = 0; p < k; p++)
                {
                    float a = A[i * k + p];
                    int boff = p * n;
                    int coff = i * n;
                    for (int j = 0; j < n; j++)
                        C[coff + j] += a * B[boff + j];
                }
            }
        }

        public static void SoftmaxInPlace(Span<float> v)
        {
            float max = float.NegativeInfinity;
            for (int i = 0; i < v.Length; i++) if (v[i] > max) max = v[i];
            double sum = 0.0;
            for (int i = 0; i < v.Length; i++) { var e = Math.Exp(v[i] - max); v[i] = (float)e; sum += e; }
            float inv = (float)(1.0 / sum);
            for (int i = 0; i < v.Length; i++) v[i] *= inv;
        }

        public static void GeluInPlace(Span<float> v)
        {
            // Approximate GELU
            for (int i = 0; i < v.Length; i++)
            {
                var x = v[i];
                v[i] = 0.5f * x * (1f + (float)Math.Tanh(Math.Sqrt(2.0 / Math.PI) * (x + 0.044715f * x * x * x)));
            }
        }

        public static void LayerNormInPlace(Span<float> v, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, float eps = 1e-5f)
        {
            int d = v.Length;
            double mean = 0, var = 0;
            for (int i = 0; i < d; i++) mean += v[i];
            mean /= d;
            for (int i = 0; i < d; i++) { var += (v[i] - mean) * (v[i] - mean); }
            var /= d;
            var inv = 1.0 / Math.Sqrt(var + eps);
            for (int i = 0; i < d; i++)
            {
                float xhat = (float)((v[i] - mean) * inv);
                v[i] = xhat * gamma[i] + beta[i];
            }
        }

        public static float[] Rand(int n, float scale, Random rng)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = (float)(rng.NextDouble() * 2 - 1) * scale;
            return a;
        }
    }
}
