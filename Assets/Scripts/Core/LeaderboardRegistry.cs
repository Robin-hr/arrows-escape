namespace ArrowEscape.Core
{
    /// <summary>The active ranking backend. An online module replaces the offline default at startup.</summary>
    public static class LeaderboardRegistry
    {
        private static ILeaderboardService service;
        /// <summary>Current backend (never null).</summary>
        public static ILeaderboardService Service { get => service ??= new OfflineLeaderboard(); set => service = value; }
    }
}
