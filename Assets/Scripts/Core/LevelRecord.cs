namespace ArrowEscape.Core
{
    /// <summary>Saved completions and the paired best attempt for a stable level id.</summary>
    [System.Serializable]
    public sealed class LevelRecord
    {
        /// <summary>File name without its folder or extension.</summary>
        public string id;
        /// <summary>Number of completed attempts.</summary>
        public int completions;
        /// <summary>Mistakes in the best attempt.</summary>
        public int bestMistakes;
        /// <summary>Undos in that same best attempt.</summary>
        public int bestUndos;
        /// <summary>Highest earned stars; zero means no star result recorded yet.</summary>
        public int bestStars;
    }
}
