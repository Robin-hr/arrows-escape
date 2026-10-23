namespace ArrowEscape.Core
{
    /// <summary>Which avatars and frames are premium (they animate wherever the player's badge is shown).</summary>
    public static class PremiumCosmetics
    {
        /// <summary>Premium avatars: the "Premium" shop section.</summary>
        public static bool IsPremiumAvatar(string id) { var item = ShopCatalog.Find(id); return item != null && item.Slot == ShopSlot.Avatar && item.Category == "Premium"; }
        /// <summary>Premium frames: the Magic and Mythic frames.</summary>
        public static bool IsPremiumFrame(string id) { var item = ShopCatalog.Find(id); return item != null && item.Slot == ShopSlot.Frame && (item.Category == "Magic Frames" || item.Category == "Mythic Frames"); }
    }
}
