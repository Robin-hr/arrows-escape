namespace ArrowEscape.Core
{
    /// <summary>An immutable snapshot of one logical move and the resulting mistake count.</summary>
    public sealed class MoveResult
    {
        /// <summary>Whether the arrow exited, was blocked, or was ignored.</summary>
        public MoveOutcome Outcome { get; }
        /// <summary>The arrow supplied to the move request, possibly null for an ignored request.</summary>
        public Arrow Arrow { get; }
        /// <summary>The nearest blocker for a blocked move; otherwise null.</summary>
        public Arrow Blocker { get; }
        /// <summary>True only when this move removed the last arrow.</summary>
        public bool BoardCleared { get; }
        /// <summary>The session's mistake count after the move.</summary>
        public int MistakeCount { get; }
        /// <summary>The replacement board arrow for a slide; otherwise null.</summary>
        public Arrow MovedArrow { get; }
        /// <summary>Cells advanced during a slide; otherwise zero.</summary>
        public int SlideCells { get; }
        /// <summary>Creates a slide result without changing the existing constructor contract.</summary>
        public MoveResult(Arrow before, Arrow after, int slideCells, int mistakeCount)
            : this(MoveOutcome.Slid,before,null,false,mistakeCount) { MovedArrow=after; SlideCells=slideCells; }

        /// <summary>Creates a result, keeping blocker and cleared flags consistent with the outcome.</summary>
        public MoveResult(MoveOutcome outcome, Arrow arrow, Arrow blocker, bool boardCleared, int mistakeCount)
        {
            Outcome = outcome;
            Arrow = arrow;
            Blocker = outcome == MoveOutcome.Blocked ? blocker : null;
            BoardCleared = outcome == MoveOutcome.Exited && boardCleared;
            MistakeCount = mistakeCount;
        }
    }
}
