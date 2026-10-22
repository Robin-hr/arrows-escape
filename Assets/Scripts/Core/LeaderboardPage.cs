using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>The top of the world ranking plus the local player's own row.</summary>
    public sealed class LeaderboardPage
    {
        /// <summary>Top entries, best first.</summary>
        public IReadOnlyList<LeaderboardEntry> Top { get; }
        /// <summary>The local player's row (may be outside the top), or null.</summary>
        public LeaderboardEntry Me { get; }
        /// <summary>Online, offline or failed.</summary>
        public LeaderboardStatus Status { get; }
        /// <summary>Short reason shown when not online.</summary>
        public string Message { get; }
        /// <summary>Creates a page.</summary>
        public LeaderboardPage(IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry me, LeaderboardStatus status, string message = "")
        { Top = top ?? Array.Empty<LeaderboardEntry>(); Me = me; Status = status; Message = message ?? ""; }
    }
}
