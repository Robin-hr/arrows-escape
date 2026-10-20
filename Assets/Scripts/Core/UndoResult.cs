namespace ArrowEscape.Core
{
    /// <summary>Immutable result after an undo attempt.</summary>
    public sealed class UndoResult
    {
        /// <summary>Whether restoration succeeded.</summary>
        public UndoOutcome Outcome { get; }
        /// <summary>The new arrow instance, only on success.</summary>
        public Arrow Restored { get; }
        /// <summary>Progress after this request.</summary>
        public int MoveCount { get; }
        /// <summary>The action kind, defaulting to Exit for the legacy constructor.</summary>
        public UndoMoveKind MoveKind { get; }
        /// <summary>Creates a result with an explicit history kind.</summary>
        public UndoResult(UndoOutcome outcome, Arrow restored, int moveCount, UndoMoveKind moveKind)
            : this(outcome,restored,moveCount) { MoveKind=moveKind; }
        /// <summary>Creates an undo result.</summary>
        public UndoResult(UndoOutcome outcome, Arrow restored, int moveCount)
        {
            Outcome = outcome;
            Restored = outcome == UndoOutcome.Undone ? restored : null;
            MoveCount = moveCount;
        }
    }
}
