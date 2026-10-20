using System;
namespace ArrowEscape.Core
{
    /// <summary>An immutable grid cell.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        /// <summary>Column.</summary>
        public int X { get; }
        /// <summary>Row, increasing upward.</summary>
        public int Y { get; }
        /// <summary>Creates a cell.</summary>
        public Cell(int x, int y) { X=x; Y=y; }
        /// <summary>Compares coordinates.</summary>
        public bool Equals(Cell other) => X==other.X && Y==other.Y;
        /// <summary>Compares a boxed cell.</summary>
        public override bool Equals(object other) => other is Cell cell && Equals(cell);
        /// <summary>Hash for coordinate lookups.</summary>
        public override int GetHashCode() => unchecked(X*397 ^ Y);
    }
}
