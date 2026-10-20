namespace ArrowEscape.Core
{
    /// <summary>Result of a request to restore the latest exit.</summary>
    public enum UndoOutcome
    {
        /// <summary>The arrow was restored.</summary>
        Undone,
        /// <summary>No exits are available.</summary>
        NothingToUndo,
        /// <summary>A completed attempt is final.</summary>
        LevelCleared,
        /// <summary>The shared board could not accept the arrow.</summary>
        Failed
    }
}
