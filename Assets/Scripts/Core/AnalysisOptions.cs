using System;
namespace ArrowEscape.Core
{
    /// <summary>Immutable limits for action-triggered level analysis.</summary>
    public sealed class AnalysisOptions
    {
        /// <summary>Maximum distinct states in exhaustive traversal.</summary>
        public int MaxStates { get; }
        /// <summary>Number of independent random games.</summary>
        public int Playouts { get; }
        /// <summary>Fixed seed for repeatable playout sampling.</summary>
        public int PlayoutSeed { get; }
        /// <summary>Validates positive state and playout budgets.</summary>
        public AnalysisOptions(int maxStates=50000,int playouts=128,int playoutSeed=7177)
        {
            if(maxStates<1)throw new ArgumentOutOfRangeException(nameof(maxStates));
            if(playouts<1)throw new ArgumentOutOfRangeException(nameof(playouts));
            MaxStates=maxStates;Playouts=playouts;PlayoutSeed=playoutSeed;
        }
    }
}
