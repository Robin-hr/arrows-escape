using System;
namespace ArrowEscape.Core
{
    /// <summary>Deterministic integer-only difficulty scoring, never using incomplete exhaustive counts.</summary>
    public static class DifficultyRater
    {
        /// <summary>Rates solved boards only; all component values are clamped to permille.</summary>
        public static DifficultyRating Rate(LevelAnalysis analysis)
        {
            if(analysis==null)throw new ArgumentNullException(nameof(analysis));
            if(analysis.Status!=SolveStatus.Solved)throw new InvalidOperationException("Only solved levels can be rated.");
            int size=Clamp((long)analysis.ArrowCount*DifficultyWeights.Scale/DifficultyWeights.SizeArrows);
            int scarcity=Clamp(DifficultyWeights.Scale-(long)analysis.AvgChoicePermille);
            int chain=Clamp((long)analysis.MaxBlockerChain*DifficultyWeights.Scale/DifficultyWeights.ChainLength);
            int traps=Clamp(analysis.TrapRatePermille);
            int slides=Clamp((long)analysis.SlidesInSolution*DifficultyWeights.Scale/DifficultyWeights.SlideCount);
            int score=(DifficultyWeights.SizeWeight*size+DifficultyWeights.ScarcityWeight*scarcity+
                DifficultyWeights.ChainWeight*chain+DifficultyWeights.TrapWeight*traps+DifficultyWeights.SlideWeight*slides)/DifficultyWeights.TotalWeight;
            return new DifficultyRating(Clamp(score),size,scarcity,chain,traps,slides);
        }
        /// <summary>Maps scores at the exact configured boundaries.</summary>
        public static DifficultyTier GetTier(int score)
        {
            if(score<DifficultyWeights.MediumThreshold)return DifficultyTier.Easy;
            if(score<DifficultyWeights.HardThreshold)return DifficultyTier.Medium;
            return score<DifficultyWeights.ExpertThreshold?DifficultyTier.Hard:DifficultyTier.Expert;
        }
        private static int Clamp(long value)=>(int)Math.Max(0,Math.Min(DifficultyWeights.Scale,value));
    }
}
