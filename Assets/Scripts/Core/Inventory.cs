using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Version-one inventory payload; unknown ids are retained for future versions.</summary>
    [Serializable] public sealed class Inventory
    {
        /// <summary>File format version, separate from progress.</summary>
        public int version = 1;
        /// <summary>Nonnegative integer balance.</summary>
        public int Coins;
        /// <summary>Purchased ids; catalog lookup controls whether they can be used.</summary>
        public List<string> OwnedItemIds = new List<string> { "classic", "paper" };
        /// <summary>Equipped arrow cosmetic.</summary>
        public string EquippedArrowId = "classic";
        /// <summary>Equipped background cosmetic.</summary>
        public string EquippedThemeId = "paper";
        /// <summary>Reward history by stable level id, independent of progress saves.</summary>
        public List<LevelRewardRecord> RewardedStars = new List<LevelRewardRecord>();
        /// <summary>Next id to issue; zero is the legacy uninitialized sentinel.</summary>
        public int NextAttemptId;
        /// <summary>Highest already-paid attempt; zero means none.</summary>
        public int LastPaidAttemptId;
        /// <summary>Normalizes lists, balance and slots without discarding unknown item ids.</summary>
        public void Sanitize()
        {
            Coins=Math.Max(0,Math.Min(EconomyConfig.MaxCoins,Coins));
            OwnedItemIds=OwnedItemIds??new List<string>(); RewardedStars=RewardedStars??new List<LevelRewardRecord>();
            if(!OwnedItemIds.Contains("classic"))OwnedItemIds.Add("classic");
            if(!OwnedItemIds.Contains("paper"))OwnedItemIds.Add("paper");
            if(!ValidSlot(EquippedArrowId,ShopSlot.Arrow))EquippedArrowId="classic";
            if(!ValidSlot(EquippedThemeId,ShopSlot.Theme))EquippedThemeId="paper";
            NextAttemptId=Math.Max(0,NextAttemptId);LastPaidAttemptId=Math.Max(0,LastPaidAttemptId);
            if(LastPaidAttemptId>0 && NextAttemptId<=LastPaidAttemptId && LastPaidAttemptId<int.MaxValue)NextAttemptId=LastPaidAttemptId+1;
        }
        private bool ValidSlot(string id,ShopSlot slot)
        { ShopItem item=ShopCatalog.Find(id);return item!=null&&item.Slot==slot&&OwnedItemIds.Contains(id); }
    }
}
