using System;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Trophies per cleared level and the league ladder.
    /// 3 stars = 30 trophies; 2 stars = 25..30; 1 star = 15..25 (inclusive). Random amounts come from a seeded
    /// generator keyed by the attempt, so a saved receipt always reproduces the same award.
    /// </summary>
    public static class TrophyRules
    {
        /// <summary>Trophies needed to enter each league, Bronze to Crown.</summary>
        public static readonly int[] Thresholds = { 0, 300, 900, 1800, 3200, 5000 };

        /// <summary>Trophies for a clear with the given stars.</summary>
        public static int Award(int stars, SeededRandom random)
        {
            if (stars >= 3) return 30;
            if (stars == 2) return random.NextInt(25, 31);
            if (stars == 1) return random.NextInt(15, 26);
            return 0;
        }

        /// <summary>Deterministic seed for one attempt on one level.</summary>
        public static int Seed(string levelId, int attemptId)
        {
            unchecked
            {
                int hash = (int)2166136261;
                if (levelId != null) foreach (char c in levelId) hash = (hash ^ c) * 16777619;
                return hash ^ (attemptId * 73856093);
            }
        }

        /// <summary>League for a trophy total.</summary>
        public static League LeagueFor(int trophies)
        {
            for (int i = Thresholds.Length - 1; i > 0; i--) if (trophies >= Thresholds[i]) return (League)i;
            return League.Bronze;
        }

        /// <summary>Trophies at which the next league starts, or -1 at Crown.</summary>
        public static int NextThreshold(int trophies)
        {
            int league = (int)LeagueFor(trophies);
            return league + 1 < Thresholds.Length ? Thresholds[league + 1] : -1;
        }

        /// <summary>Progress 0..1 through the current league (1 at Crown).</summary>
        public static float Progress(int trophies)
        {
            int league = (int)LeagueFor(trophies), next = NextThreshold(trophies);
            if (next < 0) return 1f;
            int start = Thresholds[league];
            return Math.Max(0f, Math.Min(1f, (trophies - start) / (float)(next - start)));
        }

        /// <summary>Single sortable ranking score: trophies first, total stars break ties.</summary>
        public static long RankingScore(int trophies, int stars) => (long)Math.Max(0, trophies) * RankingStarSlots + Math.Max(0, Math.Min(RankingStarSlots - 1, stars));
        /// <summary>Stars occupy the low digits of the ranking score.</summary>
        public const int RankingStarSlots = 10000;
    }
}
