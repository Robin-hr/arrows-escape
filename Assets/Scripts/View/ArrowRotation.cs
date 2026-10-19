using System;
using ArrowEscape.Core;

namespace ArrowEscape.View
{
    /// <summary>Converts grid directions into rotations of the upward-facing sprite.</summary>
    public static class ArrowRotation
    {
        /// <summary>Returns Unity z rotation in degrees; positive angles turn counter-clockwise.</summary>
        public static float GetRotationZ(Direction direction)
        {
            // Starting at Up, a positive quarter-turn points Left in y-up world space.
            switch (direction)
            {
                case Direction.Up: return 0;
                case Direction.Left: return 90;
                case Direction.Down: return 180;
                case Direction.Right: return -90;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
