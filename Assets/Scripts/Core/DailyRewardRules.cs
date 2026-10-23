using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Seven-day reward calendar: one claim per calendar day; the next day in the cycle is offered each new day
    /// (missing a day does not reset it). Day 7 is a random surprise.
    /// </summary>
    public static class DailyRewardRules
    {
        /// <summary>Days in the cycle.</summary>
        public const int CycleDays = 7;
        private static readonly DailyReward[] Fixed =
        {
            new DailyReward(DailyRewardKind.Coins, 300), new DailyReward(DailyRewardKind.Undo, 1), new DailyReward(DailyRewardKind.Hint, 1),
            new DailyReward(DailyRewardKind.Coins, 500), new DailyReward(DailyRewardKind.Undo, 1), new DailyReward(DailyRewardKind.Hint, 1),
        };

        /// <summary>Reward shown for a day (0-based); day 6 (the 7th) is the mystery reward and returns null.</summary>
        public static DailyReward Preview(int day) => day >= 0 && day < Fixed.Length ? Fixed[day] : null;

        /// <summary>The day (0..6) that the next claim gives.</summary>
        public static int NextDay(Inventory inventory) => ((inventory.DailyRewardDay % CycleDays) + CycleDays) % CycleDays;

        /// <summary>True when today's reward has not been claimed yet.</summary>
        public static bool CanClaim(Inventory inventory, int todayKey) => inventory.DailyRewardLastKey != todayKey;

        /// <summary>Everything the day-7 surprise can give right now (owned cosmetics are skipped).</summary>
        public static List<DailyReward> MysteryPool(Inventory inventory)
        {
            var pool = new List<DailyReward> { new DailyReward(DailyRewardKind.Undo, 1), new DailyReward(DailyRewardKind.Hint, 1),
                new DailyReward(DailyRewardKind.Coins, 100), new DailyReward(DailyRewardKind.Coins, 200), new DailyReward(DailyRewardKind.Coins, 300) };
            foreach (ShopSlot slot in new[] { ShopSlot.Arrow, ShopSlot.Frame, ShopSlot.Avatar })
            {
                var choices = new List<ShopItem>();
                foreach (ShopItem item in ShopCatalog.Items)
                    if (item.Slot == slot && item.IsAvailable && item.Price > 0 && !inventory.OwnedItemIds.Contains(item.Id)) choices.Add(item);
                if (choices.Count == 0) continue;
                // One cheapest-third pick per slot keeps the surprise fair against the shop prices.
                choices.Sort((a, b) => a.Price.CompareTo(b.Price));
                int span = System.Math.Max(1, choices.Count / 3);
                foreach (ShopItem item in choices.GetRange(0, span)) pool.Add(new DailyReward(DailyRewardKind.Item, 1, item.Id));
            }
            return pool;
        }

        /// <summary>Deterministic day-7 pick for a claim date.</summary>
        public static DailyReward RollMystery(Inventory inventory, int todayKey)
        {
            var pool = MysteryPool(inventory);
            var random = new SeededRandom(DailyPuzzleBuilder.Seed(todayKey) ^ 0x5bd1e995);
            return pool[random.NextInt(pool.Count)];
        }

        /// <summary>Claims today's reward, applies it and advances the cycle; null when already claimed today.</summary>
        public static DailyReward Claim(ShopService shop, int todayKey)
        {
            var inventory = shop.Inventory;
            if (!CanClaim(inventory, todayKey)) return null;
            int day = NextDay(inventory);
            DailyReward reward = Preview(day) ?? RollMystery(inventory, todayKey);
            Apply(shop, reward);
            inventory.DailyRewardDay = (day + 1) % CycleDays; inventory.DailyRewardLastKey = todayKey;
            return reward;
        }

        /// <summary>Gives a reward to the player.</summary>
        public static void Apply(ShopService shop, DailyReward reward)
        {
            switch (reward.Kind)
            {
                case DailyRewardKind.Coins: shop.AddCoins(reward.Amount); break;
                case DailyRewardKind.Hint: shop.AddBoost(BoostKind.Hint, reward.Amount); break;
                case DailyRewardKind.Undo: shop.AddBoost(BoostKind.Undo, reward.Amount); break;
                default: shop.GrantItem(reward.ItemId); break;
            }
        }
    }
}
