namespace ArrowEscape.Core
{
    /// <summary>Immutable level-select display data.</summary>
    public sealed class LevelTile
    {
        /// <summary>Zero-based resource order.</summary>
        public int Index { get; }
        /// <summary>One-based display number.</summary>
        public int Number => Index + 1;
        /// <summary>Stable filename id.</summary>
        public string Id { get; }
        /// <summary>Completed takes precedence over unlock state.</summary>
        public LevelTileState State { get; }
        /// <summary>Whether Continue would select this tile.</summary>
        public bool IsResume { get; }
        /// <summary>Best attempt mistakes, or -1 without a record.</summary>
        public int BestMistakes { get; }
        /// <summary>Undos in that same attempt, or -1 without a record.</summary>
        public int BestUndos { get; }
        /// <summary>Highest saved star rating, zero for old completions.</summary>
        public int BestStars { get; }
        /// <summary>Creates one display item.</summary>
        public LevelTile(int index, string id, LevelTileState state, bool isResume, int bestMistakes = -1, int bestUndos = -1, int bestStars = 0)
        { Index = index; Id = id; State = state; IsResume = isResume; BestMistakes = bestMistakes; BestUndos = bestUndos; BestStars = bestStars; }
    }
}
