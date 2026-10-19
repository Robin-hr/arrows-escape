namespace ArrowEscape.Core
{
    /// <summary>An immutable arrow occupying one grid cell.</summary>
    public sealed class Arrow
    {
        /// <summary>The arrow's unique board identifier.</summary>
        public int Id { get; }
        /// <summary>The column, increasing to the right.</summary>
        public int X { get; }
        /// <summary>The row, increasing upward.</summary>
        public int Y { get; }
        /// <summary>The direction this arrow faces.</summary>
        public Direction Direction { get; }

        /// <summary>Creates an arrow with a fixed position and direction.</summary>
        public Arrow(int id, int x, int y, Direction direction)
        {
            Id = id;
            X = x;
            Y = y;
            Direction = direction;
        }
    }
}
