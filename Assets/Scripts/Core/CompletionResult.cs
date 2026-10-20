namespace ArrowEscape.Core
{
    /// <summary>Immutable summary of a completion and its resulting paired best.</summary>
    public sealed class CompletionResult
    {
        /// <summary>Whether this attempt strictly improved the best, including a first clear.</summary>
        public bool IsNewBest { get; }
        /// <summary>Whether this is the first recorded completion.</summary>
        public bool IsFirstClear { get; }
        /// <summary>Mistakes in the best attempt after this clear.</summary>
        public int BestMistakes { get; }
        /// <summary>Undos in that same best attempt.</summary>
        public int BestUndos { get; }
        /// <summary>Total recorded completions.</summary>
        public int Completions { get; }
        /// <summary>Creates a completed-attempt summary.</summary>
        public CompletionResult(bool isNewBest, bool isFirstClear, int bestMistakes, int bestUndos, int completions)
        {
            IsNewBest = isNewBest; IsFirstClear = isFirstClear;
            BestMistakes = bestMistakes; BestUndos = bestUndos; Completions = completions;
        }
    }
}
