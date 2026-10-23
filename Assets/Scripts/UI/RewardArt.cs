using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Picture and tint for a daily reward (coins, hint, undo, arrow skin, frame or avatar).</summary>
    public static class RewardArt
    {
        /// <summary>Texture to draw for a reward.</summary>
        public static Texture2D Texture(DailyReward reward)
        {
            if (reward == null) return BrandArt.Texture("Daily/gift_box");
            switch (reward.Kind)
            {
                case DailyRewardKind.Coins: return BrandArt.Texture("icon_coin");
                case DailyRewardKind.Hint: return BrandArt.Texture("icon_hint");
                case DailyRewardKind.Undo: return BrandArt.Texture("icon_undo");
            }
            var item = ShopCatalog.Find(reward.ItemId);
            if (item == null) return BrandArt.Texture("Daily/gift_box");
            if (item.Slot == ShopSlot.Avatar) return AvatarBadge.AvatarTexture(item.Id);
            if (item.Slot == ShopSlot.Frame) return AvatarBadge.FrameTexture(item.Id);
            return BrandArt.Texture("icon_arrow");
        }
        /// <summary>Tint (arrow skins use their colour; everything else is untinted).</summary>
        public static Color Tint(DailyReward reward)
        {
            if (reward == null || reward.Kind != DailyRewardKind.Item) return Color.white;
            var item = ShopCatalog.Find(reward.ItemId);
            if (item == null || item.Slot != ShopSlot.Arrow) return Color.white;
            return ColorUtility.TryParseHtmlString(item.PrimaryColor, out Color c) ? Color.Lerp(c, Color.white, 0.25f) : Color.white;
        }
        /// <summary>Kind word shown under item rewards.</summary>
        public static string KindLabel(DailyReward reward)
        {
            if (reward == null || reward.Kind != DailyRewardKind.Item) return "";
            var item = ShopCatalog.Find(reward.ItemId);
            return item == null ? "" : item.Slot == ShopSlot.Arrow ? "ARROW SKIN" : item.Slot == ShopSlot.Frame ? "AVATAR FRAME" : "AVATAR";
        }
    }
}
