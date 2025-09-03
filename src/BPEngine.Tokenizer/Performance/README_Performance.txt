
Performance helpers were added here:

- Performance/Pooling.cs
  - PooledArray<T> : IDisposable for ArrayPool-backed buffers
  - ValueStringBuilder ref struct for low-allocation string concat

- Performance/SimdHelpers.cs
  - IsAscii(span) and CountOf(span, ch) using System.Numerics.Vector

- Performance/ByteUnicodeMap.cs
  - Array-backed byte<->unicode mapping for hot loops (faster than Dictionary)
