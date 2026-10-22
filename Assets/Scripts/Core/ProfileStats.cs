using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Read-only player summary shown on the Profile screen. Built from saved data; never changes it.</summary>
    public sealed class ProfileStats
    {
        /// <summary>Levels cleared at least once.</summary>
        public int LevelsCompleted { get; private set; }
        /// <summary>Levels in the catalog.</summary>
        public int LevelCount { get; private set; }
        /// <summary>Sum of best stars.</summary>
        public int Stars { get; private set; }
        /// <summary>Three stars per catalog level.</summary>
        public int MaxStars { get; private set; }
        /// <summary>Levels with a three-star best.</summary>
        public int PerfectLevels { get; private set; }
        /// <summary>Current coin balance.</summary>
        public int Coins { get; private set; }
        /// <summary>Owned arrow skins, including Classic.</summary>
        public int ArrowsOwned { get; private set; }
        /// <summary>Arrow skins in the shop, including Classic.</summary>
        public int ArrowsTotal { get; private set; }
        /// <summary>Owned themes, including Paper.</summary>
        public int ThemesOwned { get; private set; }
        /// <summary>Themes in the shop, including Paper.</summary>
        public int ThemesTotal { get; private set; }
        /// <summary>Display name of the equipped arrow.</summary>
        public string EquippedArrow { get; private set; } = "";
        /// <summary>Display name of the equipped theme.</summary>
        public string EquippedTheme { get; private set; } = "";

        /// <summary>Collects the summary; null inputs give an empty profile.</summary>
        public static ProfileStats Build(ProgressTracker tracker, IReadOnlyList<string> levelIds, Inventory inventory)
        {
            var stats = new ProfileStats();
            int levels = levelIds == null ? 0 : levelIds.Count;
            stats.LevelCount = levels; stats.MaxStars = levels * 3;
            if (tracker != null && levelIds != null)
            {
                stats.LevelsCompleted = tracker.CompletedCount(levelIds);
                for (int i = 0; i < levelIds.Count; i++)
                    if (tracker.TryGetRecord(levelIds[i], out LevelRecord record) && record != null)
                    { stats.Stars += record.bestStars; if (record.bestStars >= 3) stats.PerfectLevels++; }
            }
            IReadOnlyList<ShopItem> items = ShopCatalog.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ShopItem item = items[i];
                bool owned = inventory != null && inventory.OwnedItemIds != null && inventory.OwnedItemIds.Contains(item.Id);
                if (item.Slot == ShopSlot.Arrow) { stats.ArrowsTotal++; if (owned) stats.ArrowsOwned++; }
                else if (item.Slot == ShopSlot.Theme) { stats.ThemesTotal++; if (owned) stats.ThemesOwned++; }
            }
            if (inventory != null)
            {
                stats.Coins = inventory.Coins;
                stats.EquippedArrow = NameOf(inventory.EquippedArrowId);
                stats.EquippedTheme = NameOf(inventory.EquippedThemeId);
            }
            return stats;
        }

        private static string NameOf(string id)
        {
            ShopItem item = ShopCatalog.Find(id);
            return item != null ? item.Name : id ?? "";
        }
    }
}
