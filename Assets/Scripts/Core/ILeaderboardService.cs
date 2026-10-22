using System.Threading.Tasks;
namespace ArrowEscape.Core
{
    /// <summary>World ranking backend. Implementations never throw; failures return an Offline/Error page.</summary>
    public interface ILeaderboardService
    {
        /// <summary>True when a real online backend is available in this build.</summary>
        bool IsOnline { get; }
        /// <summary>Publishes the local player's ranking score (trophies first, stars break ties).</summary>
        Task SubmitAsync(PlayerSnapshot me);
        /// <summary>Fetches the top of the world ranking and the local player's row.</summary>
        Task<LeaderboardPage> FetchAsync(PlayerSnapshot me, int limit);
    }
}
