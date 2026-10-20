using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure uniform scale for a fixed-size menu inside a usable rectangle.</summary>
    public static class UiFit
    {
        /// <summary>Shrinks without enlarging; invalid available dimensions are rejected.</summary>
        public static float ComputeScale(float contentWidth, float contentHeight, float availableWidth, float availableHeight)
        {
            if (!(availableWidth > 0) || float.IsInfinity(availableWidth)) throw new ArgumentOutOfRangeException(nameof(availableWidth));
            if (!(availableHeight > 0) || float.IsInfinity(availableHeight)) throw new ArgumentOutOfRangeException(nameof(availableHeight));
            if (!(contentWidth > 0) || !(contentHeight > 0)) return 1;
            return Math.Min(1, Math.Min(availableWidth / contentWidth, availableHeight / contentHeight));
        }
    }
}
