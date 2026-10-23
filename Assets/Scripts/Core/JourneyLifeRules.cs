using System;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Journey lives: at most five; starting a Journey level costs one; one refills every <see cref="RefillMinutes"/>
    /// minutes. Refills are computed from saved UTC timestamps, so they keep coming while the game is closed.
    /// </summary>
    public static class JourneyLifeRules
    {
        /// <summary>Maximum lives.</summary>
        public const int Max = 5;
        /// <summary>Minutes for one life to refill.</summary>
        public const int RefillMinutes = 15;
        /// <summary>Ticks per refill.</summary>
        public static long RefillTicks => TimeSpan.FromMinutes(RefillMinutes).Ticks;

        /// <summary>Adds every life refilled up to <paramref name="nowTicks"/>; returns the lives now available.</summary>
        public static int Update(Inventory inventory, long nowTicks)
        {
            if (inventory.JourneyLives >= Max) { inventory.JourneyLives = Max; inventory.JourneyRefillTicks = 0; return Max; }
            if (inventory.JourneyRefillTicks <= 0 || inventory.JourneyRefillTicks > nowTicks) inventory.JourneyRefillTicks = nowTicks;
            long gained = (nowTicks - inventory.JourneyRefillTicks) / RefillTicks;
            if (gained > 0)
            {
                inventory.JourneyLives = (int)Math.Min(Max, inventory.JourneyLives + gained);
                inventory.JourneyRefillTicks = inventory.JourneyLives >= Max ? 0 : inventory.JourneyRefillTicks + gained * RefillTicks;
            }
            return inventory.JourneyLives;
        }

        /// <summary>Spends one life to start a Journey level; false when none are left.</summary>
        public static bool TrySpend(Inventory inventory, long nowTicks)
        {
            if (Update(inventory, nowTicks) <= 0) return false;
            if (inventory.JourneyLives >= Max) inventory.JourneyRefillTicks = nowTicks;
            inventory.JourneyLives--;
            return true;
        }

        /// <summary>Gives lives (ad or purchase), never above the maximum.</summary>
        public static void Add(Inventory inventory, int count, long nowTicks)
        {
            Update(inventory, nowTicks);
            inventory.JourneyLives = Math.Min(Max, inventory.JourneyLives + Math.Max(0, count));
            if (inventory.JourneyLives >= Max) inventory.JourneyRefillTicks = 0;
        }

        /// <summary>Whole seconds until the next life (0 when full).</summary>
        public static int SecondsToNext(Inventory inventory, long nowTicks)
        {
            if (Update(inventory, nowTicks) >= Max) return 0;
            long left = inventory.JourneyRefillTicks + RefillTicks - nowTicks;
            return (int)Math.Max(1, (left + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);
        }

        /// <summary>"mm:ss" countdown text.</summary>
        public static string Format(int seconds) => (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}
