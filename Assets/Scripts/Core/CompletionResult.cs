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
        /// <summary>Stars for this attempt, zero for legacy summaries.</summary>
        public int Stars { get; }
        /// <summary>Accepted taps in this attempt.</summary>
        public int Moves { get; }
        /// <summary>Fixed move budget, zero for legacy summaries.</summary>
        public int Par { get; }
        /// <summary>Highest earned stars after this completion.</summary>
        public int BestStars { get; }
        /// <summary>Whether stars strictly improved, including a first clear.</summary>
        public bool IsNewBestStars { get; }
        /// <summary>Extends an existing summary without changing paired best results.</summary>
        public CompletionResult(CompletionResult prior, int stars, int moves, int par, int bestStars, bool improved)
            : this(prior.IsNewBest, prior.IsFirstClear, prior.BestMistakes, prior.BestUndos, prior.Completions)
        { Stars = stars; Moves = moves; Par = par; BestStars = bestStars; IsNewBestStars = improved; }
        /// <summary>Creates a completed-attempt summary.</summary>
        public CompletionResult(bool isNewBest, bool isFirstClear, int bestMistakes, int bestUndos, int completions)
        {
            IsNewBest = isNewBest; IsFirstClear = isFirstClear;
            BestMistakes = bestMistakes; BestUndos = bestUndos; Completions = completions;
        }
    }
}
