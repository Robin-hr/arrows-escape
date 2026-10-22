namespace ArrowEscape.Core
{
    /// <summary>One row of the world ranking.</summary>
    public sealed class LeaderboardEntry
    {
        /// <summary>One-based world rank.</summary>
        public int Rank { get; }
        /// <summary>Stable online player id (empty offline).</summary>
        public string PlayerId { get; }
        /// <summary>Display name.</summary>
        public string Name { get; }
        /// <summary>Total trophies.</summary>
        public int Trophies { get; }
        /// <summary>Total best stars.</summary>
        public int Stars { get; }
        /// <summary>Equipped avatar id.</summary>
        public string AvatarId { get; }
        /// <summary>Equipped frame id.</summary>
        public string FrameId { get; }
        /// <summary>True for the local player.</summary>
        public bool IsMe { get; }
        /// <summary>League from the trophies.</summary>
        public League League => TrophyRules.LeagueFor(Trophies);
        /// <summary>Creates an entry.</summary>
        public LeaderboardEntry(int rank, string playerId, string name, int trophies, int stars, string avatarId, string frameId, bool isMe)
        { Rank = rank; PlayerId = playerId ?? ""; Name = string.IsNullOrEmpty(name) ? "Player" : name; Trophies = trophies; Stars = stars; AvatarId = avatarId; FrameId = frameId; IsMe = isMe; }
    }
}
