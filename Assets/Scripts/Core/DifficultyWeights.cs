namespace ArrowEscape.Core
{
    /// <summary>All score calibration lives here; changes deliberately affect generated difficulty labels.</summary>
    public static class DifficultyWeights
    {
        /// <summary>Permille and score ceiling.</summary>
        public const int Scale=1000;
        /// <summary>Arrow count at maximum size contribution.</summary>
        public const int SizeArrows=60;
        /// <summary>Blocker-chain length at maximum chain contribution.</summary>
        public const int ChainLength=12;
        /// <summary>Solver slides at maximum slide contribution.</summary>
        public const int SlideCount=6;
        /// <summary>Size weight.</summary>
        public const int SizeWeight=25;
        /// <summary>Choice scarcity weight.</summary>
        public const int ScarcityWeight=25;
        /// <summary>Nearest-blocker-chain weight.</summary>
        public const int ChainWeight=20;
        /// <summary>Random-playout trap weight.</summary>
        public const int TrapWeight=20;
        /// <summary>Authority-solution slide weight.</summary>
        public const int SlideWeight=10;
        /// <summary>Total weight denominator.</summary>
        public const int TotalWeight=100;
        /// <summary>First Medium score.</summary>
        public const int MediumThreshold=250;
        /// <summary>First Hard score.</summary>
        public const int HardThreshold=310;
        /// <summary>First Expert score.</summary>
        public const int ExpertThreshold=380;
    }
}

