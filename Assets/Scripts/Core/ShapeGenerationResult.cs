namespace ArrowEscape.Core
{
    /// <summary>Generated board and measured quality, or a clear failure reason.</summary>
    public sealed class ShapeGenerationResult
    {
        /// <summary>Whether every quality rule passed.</summary>
        public bool Success => Board!=null;
        /// <summary>Trimmed generated board; null on failure.</summary>
        public Board Board { get; }
        /// <summary>Trimmed shape mask.</summary>
        public ShapeMask Mask { get; }
        /// <summary>Requested seed.</summary>
        public int Seed { get; }
        /// <summary>Number of attempts evaluated.</summary>
        public int AttemptsUsed { get; }
        /// <summary>Covered mask cells per thousand.</summary>
        public int CoveragePermille { get; }
        /// <summary>Bounded analysis.</summary>
        public LevelAnalysis Analysis { get; }
        /// <summary>Unmodified Step 13 difficulty rating.</summary>
        public DifficultyRating Rating { get; }
        /// <summary>Whether the desired tier was reached.</summary>
        public bool TierMatched { get; }
        /// <summary>Main rejection reason on failure.</summary>
        public string FailureReason { get; }
        /// <summary>Creates a result without altering the supplied board.</summary>
        public ShapeGenerationResult(Board board,ShapeMask mask,int seed,int attempts,int coverage,LevelAnalysis analysis,DifficultyRating rating,bool matched,string failure=null)
        {Board=board;Mask=mask;Seed=seed;AttemptsUsed=attempts;CoveragePermille=coverage;Analysis=analysis;Rating=rating;TierMatched=matched;FailureReason=failure;}
    }
}
