namespace ArrowEscape.Core
{
    /// <summary>Immutable inset guide endpoints in continuous cell coordinates.</summary>
    public readonly struct GuideSegment
    {
        /// <summary>Start column.</summary>
        public float StartX { get; }
        /// <summary>Start row.</summary>
        public float StartY { get; }
        /// <summary>End column.</summary>
        public float EndX { get; }
        /// <summary>End row.</summary>
        public float EndY { get; }
        /// <summary>Positive distance between inset endpoints.</summary>
        public float Length { get; }
        /// <summary>Whether the segment lies along a row.</summary>
        public bool IsHorizontal { get; }
        /// <summary>Creates a computed segment.</summary>
        public GuideSegment(float startX, float startY, float endX, float endY, float length, bool isHorizontal)
        { StartX = startX; StartY = startY; EndX = endX; EndY = endY; Length = length; IsHorizontal = isHorizontal; }
    }
}
