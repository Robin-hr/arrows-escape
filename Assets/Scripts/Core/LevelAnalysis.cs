using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable measurements. Incomplete exhaustive counts are all zero.</summary>
    public sealed class LevelAnalysis
    {
        /// <summary>Board width.</summary>
        public int Width { get; }
        /// <summary>Board height.</summary>
        public int Height { get; }
        /// <summary>Initial arrow count.</summary>
        public int ArrowCount { get; }
        /// <summary>Initial Ice count.</summary>
        public int IceCount { get; }
        /// <summary>Authority solver status, independent of exhaustive limits.</summary>
        public SolveStatus Status { get; }
        /// <summary>Read-only authority solver path, null unless solved.</summary>
        public IReadOnlyList<SolverMove> SolverPath { get; }
        /// <summary>Slide actions in the authority path, not the generator's intended path.</summary>
        public int SlidesInSolution { get; }
        /// <summary>Initially exitable arrows.</summary>
        public int FreeExitsAtStart { get; }
        /// <summary>Initially slideable Ice arrows.</summary>
        public int SlidesAtStart { get; }
        /// <summary>Longest unique nearest-blocker chain, including the first arrow.</summary>
        public int MaxBlockerChain { get; }
        /// <summary>Whether exhaustive traversal visited the entire reachable DAG.</summary>
        public bool ExhaustiveComplete { get; }
        /// <summary>Distinct reachable states, including start and the cleared state.</summary>
        public int ReachableStates { get; }
        /// <summary>Nonempty reachable states with no clearing sequence.</summary>
        public int DeadStates { get; }
        /// <summary>Distinct clearing sequences, capped at the documented limit.</summary>
        public long SolutionCount { get; }
        /// <summary>Whether the count reached its cap.</summary>
        public bool SolutionCountSaturated { get; }
        /// <summary>Edges from solvable states to dead states.</summary>
        public int TrapMoves { get; }
        /// <summary>Legal start actions when exhaustive traversal completes.</summary>
        public int StartMoves { get; }
        /// <summary>Start actions directly entering a dead state.</summary>
        public int StartTrapMoves { get; }
        /// <summary>Failed random games per thousand.</summary>
        public int TrapRatePermille { get; }
        /// <summary>Mean legal actions per remaining arrow, scaled by one thousand.</summary>
        public int AvgChoicePermille { get; }
        /// <summary>Copies all measurements; exhaustive values are suppressed when incomplete.</summary>
        public LevelAnalysis(int width,int height,int arrowCount,int iceCount,SolveResult solver,
            int freeExitsAtStart,int slidesAtStart,int maxBlockerChain,bool exhaustiveComplete,
            int reachableStates,int deadStates,long solutionCount,bool solutionCountSaturated,
            int trapMoves,int startMoves,int startTrapMoves,int trapRatePermille,int avgChoicePermille)
        {
            Width=width;Height=height;ArrowCount=arrowCount;IceCount=iceCount;Status=solver.Status;
            SolverPath=solver.Path;
            if(SolverPath!=null)for(int i=0;i<SolverPath.Count;i++)if(SolverPath[i].IsSlide)SlidesInSolution++;
            FreeExitsAtStart=freeExitsAtStart;SlidesAtStart=slidesAtStart;MaxBlockerChain=maxBlockerChain;
            ExhaustiveComplete=exhaustiveComplete;
            if(exhaustiveComplete)
            {
                ReachableStates=reachableStates;DeadStates=deadStates;SolutionCount=solutionCount;
                SolutionCountSaturated=solutionCountSaturated;TrapMoves=trapMoves;StartMoves=startMoves;StartTrapMoves=startTrapMoves;
            }
            TrapRatePermille=trapRatePermille;AvgChoicePermille=avgChoicePermille;
        }
    }
}
