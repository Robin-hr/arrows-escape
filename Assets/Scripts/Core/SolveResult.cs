using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable bounded-search result with a defensive read-only solution path.</summary>
    public sealed class SolveResult
    {
        /// <summary>Proven solution, proven dead end, or exhausted budget.</summary>
        public SolveStatus Status { get; }
        /// <summary>Number of expanded states, never above the supplied limit.</summary>
        public int NodesVisited { get; }
        /// <summary>A complete path only when solved; null otherwise.</summary>
        public IReadOnlyList<SolverMove> Path { get; }
        /// <summary>Copies the successful path so callers cannot mutate it.</summary>
        public SolveResult(SolveStatus status,int nodesVisited,IReadOnlyList<SolverMove> path=null)
        { Status=status;NodesVisited=nodesVisited;if(status==SolveStatus.Solved){var copy=new List<SolverMove>();if(path!=null)for(int i=0;i<path.Count;i++)copy.Add(path[i]);Path=copy.AsReadOnly();} }
    }
}
