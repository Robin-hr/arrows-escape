namespace ArrowEscape.Core
{
    /// <summary>Immutable history entry for one exited arrow.</summary>
    public sealed class MoveRecord
    {
        /// <summary>The original immutable arrow, including its cell and direction.</summary>
        public Arrow Arrow { get; }
        /// <summary>Records an exit.</summary>
        public MoveRecord(Arrow arrow) => Arrow = arrow;
    }
}
