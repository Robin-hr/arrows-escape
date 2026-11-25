using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Step 17: keeps the local reminders up to date. It rebuilds the plan (<see cref="NotificationPlanner"/>) whenever the
    /// game goes to the background, a daily puzzle is solved, or the setting changes, and asks for permission once with a
    /// friendly prompt after the player has cleared a couple of levels (Android 13+ shows the system dialog after "Yes").
    /// </summary>
    public sealed class NotificationDirector : MonoBehaviour
    {
        private GameBootstrap bootstrap;
        private ConfirmDialog dialog;
        /// <summary>Levels a player clears before the soft prompt appears.</summary>
        public const int AskAfterLevels = 2;
        /// <summary>Overrides the clock in tests.</summary>
        public Func<DateTime> Clock = () => DateTime.Now;
        /// <summary>The scheduler in use.</summary>
        public INotificationScheduler Scheduler => NotificationRegistry.Scheduler;
        /// <summary>The most recent plan (for tests and the settings screen).</summary>
        public System.Collections.Generic.List<PlannedNotification> LastPlan { get; private set; } = new System.Collections.Generic.List<PlannedNotification>();

        /// <summary>Connects the game state and the dialog used for the soft prompt.</summary>
        public void Bind(GameBootstrap boot, ConfirmDialog confirm) { bootstrap = boot; dialog = confirm; }

        /// <summary>True when reminders are on, supported and allowed.</summary>
        public bool Active => bootstrap != null && bootstrap.Settings != null && bootstrap.Settings.Model.NotificationsEnabled && bootstrap.Settings.Model.NotificationsAsked
            && Scheduler.IsSupported && Scheduler.Permission != NotificationPermission.Denied;

        /// <summary>Builds what the planner needs from the saved state.</summary>
        public NotificationContext Context()
        {
            DateTime now = Clock(); var c = new NotificationContext { Now = now };
            if (bootstrap == null || bootstrap.Inventory == null) return c;
            var inv = bootstrap.Inventory.State; int key = DailyCalendar.Key(now);
            c.DailySolvedToday = DailyStreakRules.SolvedToday(inv, key);
            c.Streak = DailyStreakRules.CurrentStreak(inv, key);
            c.RewardWaiting = DailyRewardRules.CanClaim(inv, key);
            long utc = now.ToUniversalTime().Ticks;
            int lives = JourneyLifeRules.Update(inv, utc); c.JourneyLives = lives;
            if (lives < JourneyLifeRules.Max) c.SecondsToFullLives = JourneyLifeRules.SecondsToNext(inv, utc) + (JourneyLifeRules.Max - lives - 1) * (int)(JourneyLifeRules.RefillTicks / TimeSpan.TicksPerSecond);
            return c;
        }

        /// <summary>Cancels and re-schedules every reminder (or cancels all when reminders are off).</summary>
        public void Reschedule()
        {
            if (!Scheduler.IsSupported) return;
            if (!Active) { Scheduler.CancelAll(); LastPlan.Clear(); return; }
            LastPlan = NotificationPlanner.Plan(Context());
            try { Scheduler.Replace(LastPlan); } catch (Exception e) { Debug.LogWarning("Arrows Keymaster: could not schedule reminders: " + e.Message); }
        }

        /// <summary>Settings toggle: turning on asks for permission when needed.</summary>
        public void SetEnabled(bool on, Action<bool> done = null)
        {
            if (bootstrap?.Settings == null) return;
            if (!on) { bootstrap.Settings.SetNotifications(false); Reschedule(); done?.Invoke(false); return; }
            if (Scheduler.Permission == NotificationPermission.Granted) { bootstrap.Settings.SetNotifications(true); Reschedule(); done?.Invoke(true); return; }
            Scheduler.RequestPermission(granted => { bootstrap.Settings.SetNotifications(granted); Reschedule(); done?.Invoke(granted); });
        }

        /// <summary>Shows the friendly prompt once, after <see cref="AskAfterLevels"/> cleared levels.</summary>
        public bool MaybeAsk(int completedLevels)
        {
            if (bootstrap?.Settings == null || dialog == null || dialog.IsOpen || bootstrap.Settings.Model.NotificationsAsked || !Scheduler.IsSupported || completedLevels < AskAfterLevels) return false;
            dialog.Show("Daily reminders?", "Get a nudge when the new Daily Puzzle is ready, when your streak is in danger and when a special offer starts.",
                "Yes, remind me", () => SetEnabled(true), () => SetEnabled(false), "Not now");
            return true;
        }

        private void OnApplicationPause(bool paused) { if (paused) Reschedule(); }
        private void OnApplicationQuit() => Reschedule();
    }
}
