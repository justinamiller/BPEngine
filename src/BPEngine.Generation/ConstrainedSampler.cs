using System;

namespace BPEngine.Generation
{
    public static class ConstrainedSampler
    {
        /// <summary>
        /// Masks logits for tokens that would violate the constraint.
        /// getPiece(id) must return the token's decoded piece (cheap lookup via decode map).
        /// </summary>
        public static void ApplyConstraint(float[] logits, int vocabSize, Func<int, string> getPiece, IConstrainedDecoder constraint)
        {
            // We assume logits array ends with the last-step vocab slice [.. vocabSize]
            var start = logits.Length - vocabSize;
            for (int i = 0; i < vocabSize; i++)
            {
                var piece = getPiece(i);
                if (!constraint.Accept(piece))
                    logits[start + i] = float.NegativeInfinity;
            }
        }
    }
}
