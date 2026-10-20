using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable outcome container; a successful Board remains an ordinary mutable gameplay board.</summary>
    public sealed class GenerationResult
    {
        /// <summary>Whether any candidate passed every quality filter.</summary>
        public bool Success { get; }
        /// <summary>Generated board, null on failure.</summary>
        public Board Board { get; }
        /// <summary>User-supplied seed.</summary>
        public int Seed { get; }
        /// <summary>Total candidates attempted, including attempts after a fallback candidate.</summary>
        public int AttemptsUsed { get; }
        /// <summary>Analysis of the selected candidate.</summary>
        public LevelAnalysis Analysis { get; }
        /// <summary>Rating of the selected candidate.</summary>
        public DifficultyRating Rating { get; }
        /// <summary>Whether the selected candidate met the requested tier.</summary>
        public bool TierMatched { get; }
        /// <summary>Reversed construction operations, remapped to reading-order ids.</summary>
        public IReadOnlyList<SolverMove> IntendedSolution { get; }
        /// <summary>Main rejection cause on failure; null on success.</summary>
        public string FailureReason { get; }
        internal GenerationResult(Board board,int seed,int attempts,LevelAnalysis analysis,DifficultyRating rating,
            bool matched,IReadOnlyList<SolverMove> intended,string failure=null)
        {
            Success=board!=null;Board=board;Seed=seed;AttemptsUsed=attempts;Analysis=analysis;Rating=rating;
            TierMatched=Success&&matched;FailureReason=Success?null:failure;
            var copy=new List<SolverMove>();if(intended!=null)for(int i=0;i<intended.Count;i++)copy.Add(intended[i]);IntendedSolution=copy.AsReadOnly();
        }
    }
}
