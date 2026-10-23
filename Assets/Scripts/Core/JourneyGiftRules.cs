using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>
    /// The gift box after a Journey level is cleared for the first time: a hint, an undo, coins, or (rarer) an
    /// unowned arrow skin. The pick is deterministic for a level and attempt, so a duplicate callback gives the same.
    /// </summary>
    public static class JourneyGiftRules
    {
        /// <summary>Rolls the gift (not yet applied).</summary>
        public static DailyReward Roll(Inventory inventory, string levelId, int attemptId)
        {
            var random = new SeededRandom(Hash(levelId) ^ (attemptId * 7919));
            var skins = new List<ShopItem>();
            foreach (ShopItem item in ShopCatalog.Items)
                if (item.Slot == ShopSlot.Arrow && item.IsAvailable && item.Price > 0 && !inventory.OwnedItemIds.Contains(item.Id)) skins.Add(item);
            skins.Sort((a, b) => a.Price.CompareTo(b.Price));
            int roll = random.NextInt(100);
            if (roll < 25) return new DailyReward(DailyRewardKind.Hint, 1);
            if (roll < 50) return new DailyReward(DailyRewardKind.Undo, 1);
            if (roll < 85 || skins.Count == 0) return new DailyReward(DailyRewardKind.Coins, 50 + 25 * random.NextInt(5));
            int span = System.Math.Max(1, skins.Count / 2);
            return new DailyReward(DailyRewardKind.Item, 1, skins[random.NextInt(span)].Id);
        }

        private static int Hash(string text)
        {
            unchecked { int h = 17; if (text != null) foreach (char c in text) h = h * 31 + c; return h & 0x7FFFFFFF; }
        }
    }
}
