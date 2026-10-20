namespace ArrowEscape.Core
{
    /// <summary>The logical result of trying to move one arrow.</summary>
    public enum MoveOutcome
    {
        /// <summary>The arrow was removed from the board immediately.</summary>
        Exited,
        /// <summary>The arrow could not exit and one mistake was counted.</summary>
        Blocked,
        /// <summary>The request did not refer to a current board member.</summary>
        Ignored
    }
}
