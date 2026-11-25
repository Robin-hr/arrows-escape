using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>One local notification to schedule.</summary>
    public sealed class PlannedNotification
    {
        /// <summary>Stable id (kind + day), so rescheduling replaces rather than duplicates.</summary>
        public int Id { get; }
        /// <summary>Android channel id (see <see cref="NotificationPlanner"/> channel constants).</summary>
        public string Channel { get; }
        /// <summary>Notification title.</summary>
        public string Title { get; }
        /// <summary>Notification text.</summary>
        public string Body { get; }
        /// <summary>Local fire time.</summary>
        public DateTime Fire { get; }
        /// <summary>Creates a plan entry.</summary>
        public PlannedNotification(int id, string channel, string title, string body, DateTime fire) { Id = id; Channel = channel; Title = title; Body = body; Fire = fire; }
        /// <inheritdoc/>
        public override string ToString() => $"{Fire:yyyy-MM-dd HH:mm} [{Channel}] {Title} - {Body}";
    }

    /// <summary>What the planner needs to know about the player right now.</summary>
    public sealed class NotificationContext
    {
        /// <summary>Local time now.</summary>
        public DateTime Now;
        /// <summary>Today's daily puzzle is already solved.</summary>
        public bool DailySolvedToday;
        /// <summary>Current daily streak (0 = none).</summary>
        public int Streak;
        /// <summary>Journey lives now.</summary>
        public int JourneyLives = JourneyLifeRules.Max;
        /// <summary>Seconds until the Journey lives are full again (0 = already full).</summary>
        public int SecondsToFullLives;
        /// <summary>Today's daily reward is still unclaimed.</summary>
        public bool RewardWaiting;
    }

    /// <summary>
    /// Step 17: decides the local reminders (pure, deterministic). Rescheduled whenever the app goes to the background,
    /// so "come back" reminders only fire while the player is away:
    /// - Daily Puzzle: every day at 09:00 for a week (today too unless already solved or past 09:00).
    /// - Streak: 20:00 today when a streak is alive but today's puzzle is not solved.
    /// - Journey lives: when all five lives are back.
    /// - Special offers: 10:00 on the first day of each upcoming sale or festival gift (two weeks ahead),
    ///   and 18:00 on the last day of a running offer ("last chance").
    /// - Missed you: 18:30 three days from now (moves forward every time the game is opened).
    /// </summary>
    public static class NotificationPlanner
    {
        /// <summary>Channel for the daily puzzle and streak reminders.</summary>
        public const string DailyChannel = "daily_puzzle";
        /// <summary>Channel for special offers.</summary>
        public const string OffersChannel = "special_offers";
        /// <summary>Channel for game reminders (lives, come back).</summary>
        public const string RemindersChannel = "reminders";
        /// <summary>Hard cap on scheduled notifications.</summary>
        public const int MaxNotifications = 24;
        private static readonly string[] DailyLines =
        {
            "Today's Daily Puzzle is ready. Can you clear it without losing a heart?",
            "A fresh Daily Puzzle just dropped. Free the keys, open the locks!",
            "New day, new maze. Your Daily Puzzle is waiting.",
            "Keymasters solve one a day. Today's puzzle is ready.",
            "The arrows are packed tight today. Up for the challenge?",
            "Your Daily Puzzle is live. Keep your brain sharp!",
            "Today's padlocks won't open themselves. Daily Puzzle ready!",
        };

        /// <summary>Builds the plan for a context (sorted by fire time, capped).</summary>
        public static List<PlannedNotification> Plan(NotificationContext c)
        {
            var list = new List<PlannedNotification>();
            DateTime today = c.Now.Date;
            for (int d = 0; d < 7; d++)
            {
                DateTime fire = today.AddDays(d).AddHours(9);
                if (fire <= c.Now || (d == 0 && c.DailySolvedToday)) continue;
                string body = DailyLines[(today.AddDays(d).DayOfYear) % DailyLines.Length];
                if (d == 0 && c.RewardWaiting) body = "Your Daily Puzzle and today's free reward are waiting!";
                list.Add(new PlannedNotification(Id(1, today.AddDays(d)), DailyChannel, "Daily Puzzle ready", body, fire));
            }
            DateTime streakFire = today.AddHours(20);
            if (c.Streak > 0 && !c.DailySolvedToday && streakFire > c.Now)
                list.Add(new PlannedNotification(Id(2, today), DailyChannel, $"Your {c.Streak}-day streak ends at midnight", "Solve today's Daily Puzzle to keep it alive.", streakFire));
            if (c.JourneyLives < JourneyLifeRules.Max && c.SecondsToFullLives > 0)
                list.Add(new PlannedNotification(Id(3, today), RemindersChannel, "Journey lives are full", "All 5 lives are back. Your Journey continues!", c.Now.AddSeconds(c.SecondsToFullLives)));
            foreach (var offer in OfferCalendar.Upcoming(today, 14))
                list.Add(new PlannedNotification(Id(4, offer.Start), OffersChannel, offer.Title + " is live!", offer.Pitch + " in the shop.", offer.Start.AddHours(10)));
            var running = OfferCalendar.ActiveOn(today);
            if (running != null && running.End > running.Start && running.End.AddHours(18) > c.Now)
                list.Add(new PlannedNotification(Id(5, running.End), OffersChannel, "Last chance: " + running.Title, running.DiscountPercent + "% off ends tonight." + (running.GiftCoins > 0 ? " Claim your free coins in the shop!" : ""), running.End.AddHours(18)));
            list.Add(new PlannedNotification(Id(6, today), RemindersChannel, "The arrows miss you", "Come back and beat your next level. A quick puzzle is waiting.", today.AddDays(3).AddHours(18).AddMinutes(30)));
            list.Sort((a, b) => a.Fire.CompareTo(b.Fire));
            if (list.Count > MaxNotifications) list.RemoveRange(MaxNotifications, list.Count - MaxNotifications);
            return list;
        }
        // kind * 10^8 + yymmdd keeps ids unique and stable per kind and day, well inside the int range.
        private static int Id(int kind, DateTime day) => kind * 100000000 + (day.Year % 100) * 10000 + day.Month * 100 + day.Day;
    }

    /// <summary>Permission state for posting notifications.</summary>
    public enum NotificationPermission { Unknown, Granted, Denied, NotSupported }

    /// <summary>Platform notification scheduler (Android in builds; a recording stub in the Editor and tests).</summary>
    public interface INotificationScheduler
    {
        /// <summary>True when this platform can show notifications.</summary>
        bool IsSupported { get; }
        /// <summary>Current permission.</summary>
        NotificationPermission Permission { get; }
        /// <summary>Asks the system for permission (Android 13+); calls back with the result.</summary>
        void RequestPermission(Action<bool> done);
        /// <summary>Cancels every scheduled notification and schedules the plan.</summary>
        void Replace(IReadOnlyList<PlannedNotification> plan);
        /// <summary>Cancels everything (reminders turned off).</summary>
        void CancelAll();
    }

    /// <summary>Recording scheduler used in the Editor and tests.</summary>
    public sealed class MemoryNotificationScheduler : INotificationScheduler
    {
        /// <summary>The last plan given to <see cref="Replace"/>.</summary>
        public List<PlannedNotification> Scheduled { get; } = new List<PlannedNotification>();
        /// <summary>Number of permission requests.</summary>
        public int PermissionRequests { get; private set; }
        /// <summary>Answer given to permission requests.</summary>
        public bool GrantOnRequest = true;
        /// <inheritdoc/>
        public bool IsSupported { get; set; } = true;
        /// <inheritdoc/>
        public NotificationPermission Permission { get; set; } = NotificationPermission.Unknown;
        /// <inheritdoc/>
        public void RequestPermission(Action<bool> done) { PermissionRequests++; Permission = GrantOnRequest ? NotificationPermission.Granted : NotificationPermission.Denied; done?.Invoke(GrantOnRequest); }
        /// <inheritdoc/>
        public void Replace(IReadOnlyList<PlannedNotification> plan) { Scheduled.Clear(); Scheduled.AddRange(plan); }
        /// <inheritdoc/>
        public void CancelAll() => Scheduled.Clear();
    }

    /// <summary>Holds the active scheduler; the Android assembly registers itself at startup.</summary>
    public static class NotificationRegistry
    {
        private static INotificationScheduler scheduler;
        /// <summary>The active scheduler (an unsupported memory stub until the Android one registers; tests assign their own).</summary>
        public static INotificationScheduler Scheduler { get => scheduler ??= new MemoryNotificationScheduler { IsSupported = false }; set => scheduler = value; }
    }
}
