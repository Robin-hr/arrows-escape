using System;

namespace ArrowEscape.Core
{
    /// <summary>The four directions an arrow can face.</summary>
    public enum Direction { Up, Down, Left, Right }

    /// <summary>Grid offsets and opposite directions.</summary>
    public static class DirectionExtensions
    {
        /// <summary>Returns the horizontal step.</summary>
        public static int GetDx(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: case Direction.Down: return 0;
                case Direction.Left: return -1;
                case Direction.Right: return 1;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>Returns the vertical step (up is positive).</summary>
        public static int GetDy(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 1;
                case Direction.Down: return -1;
                case Direction.Left: case Direction.Right: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>Returns the direction facing the other way.</summary>
        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
