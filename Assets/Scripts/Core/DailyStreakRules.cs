using System;
using System.Text;
namespace ArrowEscape.Core
{
    /// <summary>Daily-puzzle streak (consecutive solved days), streak badges and the share text.</summary>
    public static class DailyStreakRules
    {
        /// <summary>Badge thresholds: 100, 200 … 1,000 days.</summary>
        public static readonly int[] Badges = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };
        private static readonly string[] badgeNames = { "ROOKIE", "APPRENTICE", "SKILLED", "PRO", "EXPERT", "VETERAN", "MASTER", "CHAMPION", "LEGEND", "ELITE" };
        /// <summary>Rank name of a streak badge, from Rookie (100 days) to Elite (1,000 days).</summary>
        public static string BadgeName(int days) { int i = System.Array.IndexOf(Badges, days); return i < 0 ? "" : badgeNames[i]; }

        /// <summary>Streak still alive today (solved today or yesterday), otherwise 0.</summary>
        public static int CurrentStreak(Inventory inventory, int todayKey)
        {
            if (inventory.DailyLastSolvedKey == 0) return 0;
            return inventory.DailyLastSolvedKey == todayKey || inventory.DailyLastSolvedKey == DailyCalendar.AddDays(todayKey, -1) ? inventory.DailyStreak : 0;
        }

        /// <summary>True when today's daily puzzle is already solved.</summary>
        public static bool SolvedToday(Inventory inventory, int todayKey) => inventory.DailyLastSolvedKey == todayKey;

        /// <summary>Records today's solve; returns the new streak. A second solve on the same day changes nothing.</summary>
        public static int RecordSolve(Inventory inventory, int todayKey)
        {
            if (inventory.DailyLastSolvedKey == todayKey) return inventory.DailyStreak;
            inventory.DailyStreak = inventory.DailyLastSolvedKey == DailyCalendar.AddDays(todayKey, -1) ? inventory.DailyStreak + 1 : 1;
            inventory.DailyLastSolvedKey = todayKey; inventory.DailySolvedCount++;
            inventory.DailyBestStreak = Math.Max(inventory.DailyBestStreak, inventory.DailyStreak);
            return inventory.DailyStreak;
        }

        /// <summary>Number of badges earned with a best streak.</summary>
        public static int EarnedBadges(int bestStreak) { int n = 0; foreach (int b in Badges) if (bestStreak >= b) n++; return n; }

        /// <summary>Next badge threshold, or -1 when all are earned.</summary>
        public static int NextBadge(int bestStreak) { foreach (int b in Badges) if (bestStreak < b) return b; return -1; }

        /// <summary>Short spoiler-free result for sharing.</summary>
        public static string ShareText(int dateKey, int moves, int mistakes, int heartsLeft, int streak)
        {
            var text = new StringBuilder();
            text.Append("Arrows Escape Daily · ").Append(DailyCalendar.Pretty(dateKey)).Append('\n');
            text.Append("Solved in ").Append(moves).Append(moves == 1 ? " move" : " moves").Append('\n');
            for (int i = 0; i < 3; i++) text.Append(i < heartsLeft ? "❤️" : "🖤");
            text.Append(' ').Append(mistakes).Append(mistakes == 1 ? " mistake" : " mistakes").Append('\n');
            text.Append("🔥 ").Append(streak).Append(streak == 1 ? " day streak" : " day streak");
            return text.ToString();
        }
    }
}
