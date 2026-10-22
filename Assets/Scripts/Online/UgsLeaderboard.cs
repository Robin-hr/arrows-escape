using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrowEscape.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using Entry = ArrowEscape.Core.LeaderboardEntry;

namespace ArrowEscape.Online
{
    /// <summary>
    /// World ranking on Unity Gaming Services Leaderboards. Signs in anonymously on first use and never throws:
    /// when the project is not linked to Unity Cloud or the device is offline it returns an Offline/Error page.
    /// </summary>
    public sealed class UgsLeaderboard : ILeaderboardService
    {
        /// <summary>Leaderboard id created in the Unity Cloud dashboard (sort: descending, strategy: keep latest).</summary>
        public const string LeaderboardId = "world_ranking";
        private const string OfflineMessage = "World ranking is offline right now. Your trophies are saved and will be ranked when you are back online.";
        private Task<bool> ready;
        private bool warned;

        /// <inheritdoc/>
        public bool IsOnline => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => LeaderboardRegistry.Service = new UgsLeaderboard();

        private Task<bool> EnsureReady()
        {
            if (ready == null || ready.IsCompleted && !ready.Result) ready = SignIn();
            return ready;
        }

        private async Task<bool> SignIn()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception e) { Warn(e); return false; }
        }

        private void Warn(Exception e)
        {
            if (warned) return; warned = true;
            Debug.LogWarning("ArrowEscape world ranking unavailable: " + e.Message);
        }

        /// <inheritdoc/>
        public async Task SubmitAsync(PlayerSnapshot me)
        {
            if (me == null || !await EnsureReady()) return;
            try
            {
                var metadata = new RankingMetadata { avatar = me.AvatarId, frame = me.FrameId, trophies = me.Trophies, stars = me.Stars };
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, me.Score, new AddPlayerScoreOptions { Metadata = metadata });
            }
            catch (Exception e) { Warn(e); }
        }

        /// <inheritdoc/>
        public async Task<LeaderboardPage> FetchAsync(PlayerSnapshot me, int limit)
        {
            Entry local = me == null ? null : new Entry(0, "", "YOU", me.Trophies, me.Stars, me.AvatarId, me.FrameId, true);
            if (!await EnsureReady()) return new LeaderboardPage(local == null ? null : new[] { local }, local, LeaderboardStatus.Offline, OfflineMessage);
            string myId = AuthenticationService.Instance.PlayerId;
            try
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = Math.Max(1, Math.Min(100, limit)), IncludeMetadata = true });
                var top = new List<Entry>();
                foreach (var row in page.Results) top.Add(Convert(row, myId));
                Entry mine = top.Find(e => e.IsMe);
                if (mine == null)
                {
                    try { mine = Convert(await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId, new GetPlayerScoreOptions { IncludeMetadata = true }), myId); }
                    catch (Exception) { mine = local; } // No score submitted yet.
                }
                return new LeaderboardPage(top, mine ?? local, LeaderboardStatus.Online);
            }
            catch (Exception e)
            {
                Warn(e);
                return new LeaderboardPage(local == null ? null : new[] { local }, local, LeaderboardStatus.Error, OfflineMessage);
            }
        }

        private static Entry Convert(Unity.Services.Leaderboards.Models.LeaderboardEntry row, string myId)
        {
            RankingMetadata meta = null;
            if (!string.IsNullOrEmpty(row.Metadata)) { try { meta = JsonUtility.FromJson<RankingMetadata>(row.Metadata); } catch (Exception) { meta = null; } }
            long score = (long)Math.Max(0, Math.Round(row.Score));
            int trophies = (int)Math.Min(int.MaxValue, score / TrophyRules.RankingStarSlots), stars = (int)(score % TrophyRules.RankingStarSlots);
            string name = string.IsNullOrEmpty(row.PlayerName) ? "Player" : row.PlayerName;
            return new Entry(row.Rank + 1, row.PlayerId, name, trophies, stars, meta?.avatar, meta?.frame, row.PlayerId == myId);
        }
    }
}
