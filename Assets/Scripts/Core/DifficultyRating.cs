namespace ArrowEscape.Core
{
    /// <summary>Immutable integer score with separately inspectable normalized components.</summary>
    public sealed class DifficultyRating
    {
        /// <summary>Weighted score in 0..1000.</summary>
        public int Score { get; }
        /// <summary>Band selected by the score thresholds.</summary>
        public DifficultyTier Tier { get; }
        /// <summary>Arrow-count component.</summary>
        public int Size { get; }
        /// <summary>Low-choice component.</summary>
        public int Scarcity { get; }
        /// <summary>Nearest-blocker-chain component.</summary>
        public int Chain { get; }
        /// <summary>Failed-playout component.</summary>
        public int Traps { get; }
        /// <summary>Solver slide-count component.</summary>
        public int Slides { get; }
        internal DifficultyRating(int score,int size,int scarcity,int chain,int traps,int slides)
        {Score=score;Tier=DifficultyRater.GetTier(score);Size=size;Scarcity=scarcity;Chain=chain;Traps=traps;Slides=slides;}
    }
}
