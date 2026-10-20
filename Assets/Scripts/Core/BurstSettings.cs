namespace ArrowEscape.Core
{
    /// <summary>Configuration for a stratified cone or full-circle burst.</summary>
    public struct BurstSettings
    {
        /// <summary>Requested particle count.</summary>
        public int Count;
        /// <summary>Cone center, degrees counterclockwise from Right.</summary>
        public float DirectionDegrees;
        /// <summary>Half width; 180 covers a full circle.</summary>
        public float ConeHalfAngleDegrees;
        /// <summary>Shortest travel distance.</summary>
        public float MinDistance;
        /// <summary>Longest travel distance.</summary>
        public float MaxDistance;
        /// <summary>Shortest lifetime.</summary>
        public float MinLifetime;
        /// <summary>Longest lifetime.</summary>
        public float MaxLifetime;
        /// <summary>Starting size.</summary>
        public float StartSize;
        /// <summary>Ending size.</summary>
        public float EndSize;
        /// <summary>Number of palette entries.</summary>
        public int ColorCount;
    }
}
