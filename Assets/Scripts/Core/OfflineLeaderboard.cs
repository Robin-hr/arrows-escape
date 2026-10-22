using System.Threading.Tasks;
namespace ArrowEscape.Core
{
    /// <summary>Used when no online backend is configured: shows only the local player, clearly marked offline.</summary>
    public sealed class OfflineLeaderboard : ILeaderboardService
    {
        /// <summary>The reason shown in the ranking screen.</summary>
        public string Reason { get; set; } = "World ranking is offline. Your progress is saved and will be ranked once online.";
        /// <inheritdoc/>
        public bool IsOnline => false;
        /// <inheritdoc/>
        public Task SubmitAsync(PlayerSnapshot me) => Task.CompletedTask;
        /// <inheritdoc/>
        public Task<LeaderboardPage> FetchAsync(PlayerSnapshot me, int limit)
        {
            var row = me == null ? null : new LeaderboardEntry(0, "", me.Name, me.Trophies, me.Stars, me.AvatarId, me.FrameId, true);
            return Task.FromResult(new LeaderboardPage(row == null ? null : new[] { row }, row, LeaderboardStatus.Offline, Reason));
        }
    }
}
