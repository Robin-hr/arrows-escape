using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Integer-only SplitMix64; unsigned overflow is intentional and platform independent.</summary>
    public sealed class SeededRandom
    {
        private ulong state;
        /// <summary>Uses the seed's exact unsigned 32-bit representation.</summary>
        public SeededRandom(int seed) { state=unchecked((uint)seed); }
        private ulong Next()
        {
            unchecked
            {
                state+=0x9E3779B97F4A7C15UL;ulong z=state;
                z=(z^(z>>30))*0xBF58476D1CE4E5B9UL;
                z=(z^(z>>27))*0x94D049BB133111EBUL;return z^(z>>31);
            }
        }
        private ulong Bounded(ulong bound)
        {
            ulong threshold=unchecked(0UL-bound)%bound,value;
            do { value=Next(); } while(value<threshold);
            return value%bound;
        }
        /// <summary>Uniform integer in [0,maxExclusive), with rejection rather than modulo bias.</summary>
        public int NextInt(int maxExclusive)
        {
            if(maxExclusive<=0)throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)Bounded((ulong)maxExclusive);
        }
        /// <summary>Uniform integer in [min,maxExclusive), including ranges spanning zero.</summary>
        public int NextInt(int min,int maxExclusive)
        {
            if(min>=maxExclusive)throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(min+(long)Bounded((ulong)((long)maxExclusive-min)));
        }
        /// <summary>Picks an index proportional to nonnegative integer weights.</summary>
        public int NextWeighted(IReadOnlyList<int> weights)
        {
            if(weights==null)throw new ArgumentNullException(nameof(weights));
            ulong total=0;
            for(int i=0;i<weights.Count;i++)
            {
                if(weights[i]<0)throw new ArgumentOutOfRangeException(nameof(weights));
                total+=(ulong)weights[i];
            }
            if(total==0)throw new ArgumentOutOfRangeException(nameof(weights),"At least one weight must be positive.");
            ulong pick=Bounded(total);
            for(int i=0;i<weights.Count;i++){if(pick<(ulong)weights[i])return i;pick-=(ulong)weights[i];}
            throw new InvalidOperationException("Invalid weighted selection.");
        }
    }
}
