namespace ArrowEscape.Core
{
    /// <summary>What the local player publishes to the world ranking.</summary>
    public sealed class PlayerSnapshot
    {
        /// <summary>Display name.</summary>
        public string Name { get; }
        /// <summary>Total trophies.</summary>
        public int Trophies { get; }
        /// <summary>Total best stars.</summary>
        public int Stars { get; }
        /// <summary>Equipped avatar.</summary>
        public string AvatarId { get; }
        /// <summary>Equipped frame.</summary>
        public string FrameId { get; }
        /// <summary>Creates a snapshot.</summary>
        public PlayerSnapshot(string name, int trophies, int stars, string avatarId, string frameId)
        { Name = name; Trophies = trophies; Stars = stars; AvatarId = avatarId; FrameId = frameId; }
        /// <summary>The single sortable score.</summary>
        public long Score => TrophyRules.RankingScore(Trophies, Stars);
    }
}
