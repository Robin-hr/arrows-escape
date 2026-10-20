namespace ArrowEscape.Core
{
    /// <summary>Immutable best free-arrow suggestion and its dependency scores.</summary>
    public sealed class HintResult
    {
        /// <summary>No available suggestion.</summary>
        public static HintResult None { get; } = new HintResult(false, null, 0, 0);
        /// <summary>Whether a free arrow was found.</summary>
        public bool HasHint { get; }
        /// <summary>The suggested board instance, or null.</summary>
        public Arrow Arrow { get; }
        /// <summary>Other arrows freed immediately by removing this arrow.</summary>
        public int NewlyFreed { get; }
        /// <summary>Other arrows whose forward path contains this arrow.</summary>
        public int Dependents { get; }
        /// <summary>Creates a suggestion without changing its arrow.</summary>
        public HintResult(bool hasHint, Arrow arrow, int newlyFreed, int dependents)
        {
            HasHint = hasHint && arrow != null;
            Arrow = HasHint ? arrow : null;
            NewlyFreed = HasHint ? newlyFreed : 0;
            Dependents = HasHint ? dependents : 0;
        }
    }
}
