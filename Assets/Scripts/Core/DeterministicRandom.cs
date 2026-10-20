namespace ArrowEscape.Core
{
    /// <summary>Seeded xorshift32. Keep this mutable struct in a local variable or pass it by ref.</summary>
    public struct DeterministicRandom
    {
        private uint state;
        /// <summary>Starts a repeatable sequence; zero uses a fixed nonzero seed.</summary>
        public DeterministicRandom(int seed) => state = seed == 0 ? 0x6D2B79F5u : unchecked((uint)seed);
        /// <summary>Returns a value in [0,1), using 24 exactly representable random bits.</summary>
        public float NextFloat()
        {
            if (state == 0) state = 0x6D2B79F5u;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state >> 8) * (1f / 16777216f);
        }
        /// <summary>Interpolates between the supplied limits.</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();
    }
}
