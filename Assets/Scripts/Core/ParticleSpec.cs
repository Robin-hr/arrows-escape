namespace ArrowEscape.Core
{
    /// <summary>A deterministic particle's direction, travel, lifetime, size and palette index.</summary>
    public struct ParticleSpec
    {
        /// <summary>Unit direction x.</summary>
        public float DirX;
        /// <summary>Unit direction y.</summary>
        public float DirY;
        /// <summary>Total travel in cells.</summary>
        public float Distance;
        /// <summary>Lifetime in seconds.</summary>
        public float Lifetime;
        /// <summary>Starting size in cells.</summary>
        public float StartSize;
        /// <summary>Ending size in cells.</summary>
        public float EndSize;
        /// <summary>Index into the supplied palette.</summary>
        public int ColorIndex;
    }
}
