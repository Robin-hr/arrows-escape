namespace ArrowEscape.Core
{
    /// <summary>Immutable history entry for one exit or ice slide.</summary>
    public sealed class MoveRecord
    {
        /// <summary>The original immutable arrow, including its cell and direction.</summary>
        public Arrow Arrow { get; }
        /// <summary>Which action this record reverses.</summary>
        public UndoMoveKind Kind { get; }
        /// <summary>The immutable state before the action.</summary>
        public Arrow Before { get; }
        /// <summary>The replacement after a slide, or null after an exit.</summary>
        public Arrow After { get; }
        /// <summary>Records an exit.</summary>
        public MoveRecord(Arrow arrow) { Arrow=Before=arrow; Kind=UndoMoveKind.Exit; }
        private MoveRecord(Arrow before, Arrow after) { Arrow=Before=before; After=after; Kind=UndoMoveKind.Slide; }
        /// <summary>Records a slide without retaining mutable board state.</summary>
        public static MoveRecord ForSlide(Arrow before, Arrow after) => new MoveRecord(before,after);
    }
}

