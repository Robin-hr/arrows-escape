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
        public List<string> OwnedItemIds = new List<string> { "classic", "paper", DefaultAvatarId, DefaultFrameId, TapSoundSynth.DefaultId };
        /// <summary>Equipped arrow cosmetic.</summary>
        public string EquippedArrowId = "classic";
        /// <summary>Equipped background cosmetic.</summary>
        public string EquippedThemeId = "paper";
        /// <summary>Free profile avatar every player owns.</summary>
        public const string DefaultAvatarId = "avatar_player";
        /// <summary>Free avatar frame every player owns.</summary>
        public const string DefaultFrameId = "frame_bronze";
        /// <summary>Total trophies; decides the player's league.</summary>
        public int Trophies;
        /// <summary>Hints left (limited helper).</summary>
        public int Hints = EconomyConfig.StartingHints;
        /// <summary>Undos left (limited helper).</summary>
        public int Undos = EconomyConfig.StartingUndos;
        /// <summary>Next day (0..6) of the 7-day reward calendar.</summary>
        public int DailyRewardDay;
        /// <summary>yyyymmdd of the last reward claim (0 = never).</summary>
        public int DailyRewardLastKey;
        /// <summary>Daily-puzzle streak as of the last solve.</summary>
        public int DailyStreak;
        /// <summary>Best daily-puzzle streak ever (badges).</summary>
        public int DailyBestStreak;
        /// <summary>yyyymmdd of the last solved daily puzzle (0 = never).</summary>
        public int DailyLastSolvedKey;
        /// <summary>Daily puzzles solved in total.</summary>
        public int DailySolvedCount;
        /// <summary>Journey lives left (0..5); they refill over time, also while the game is closed.</summary>
        public int JourneyLives = JourneyLifeRules.Max;
        /// <summary>UTC ticks when the current refill countdown started (0 = full).</summary>
        public long JourneyRefillTicks;
        /// <summary>The player's chosen profile name (empty = default).</summary>
        public string PlayerName = "";
        /// <summary>Equipped profile avatar.</summary>
        public string EquippedAvatarId = DefaultAvatarId;
        /// <summary>Equipped profile avatar frame.</summary>
        public string EquippedFrameId = DefaultFrameId;
        /// <summary>Equipped arrow tap sound (Step 14L shop Sounds section).</summary>
        public string EquippedSoundId = TapSoundSynth.DefaultId;
        /// <summary>Reward history by stable level id, independent of progress saves.</summary>
        public List<LevelRewardRecord> RewardedStars = new List<LevelRewardRecord>();
        /// <summary>Next id to issue; zero is the legacy uninitialized sentinel.</summary>
        public int NextAttemptId;
        /// <summary>Highest already-paid attempt; zero means none.</summary>
        public int LastPaidAttemptId;
        /// <summary>Bounded retirement receipts, saved atomically with their refunds.</summary>
        public List<string> RefundedRetiredItemIds = new List<string>();
        /// <summary>Refund overflow held until spending makes room under the existing wallet cap.</summary>
        public int PendingRetirementCoins;
        /// <summary>Retired paid items and their refunds (the thunderbolt arrow and the Step 14H scenic themes).</summary>
        public static readonly string[] RetiredIds={"thunderbolt_dart","mystic_wood","frozen_cavern","pharaohs_tomb","volcanic_underworld","neon_cyberpunk","cosmic_nebula"};
        private static readonly int[] RetiredRefunds={150,200,250,350,400,500,800};
        /// <summary>Refunds each retired paid item once and removes it, retaining all unrelated and unknown data.</summary>
        public bool ApplyRetirements()
        {
            RefundedRetiredItemIds=RefundedRetiredItemIds??new List<string>();
            if(OwnedItemIds==null)return false;bool changed=false;
            for(int i=0;i<RetiredIds.Length;i++)
            {
                string id=RetiredIds[i];if(!OwnedItemIds.Contains(id))continue;changed=true;
                if(!RefundedRetiredItemIds.Contains(id))
                {
                    int refund=RetiredRefunds[i],credited=Math.Min(refund,EconomyConfig.MaxCoins-Coins);
                    Coins+=credited;PendingRetirementCoins+=refund-credited;RefundedRetiredItemIds.Add(id);
                }
                OwnedItemIds.RemoveAll(x=>x==id);
                if(EquippedArrowId==id)EquippedArrowId="classic";
                if(EquippedThemeId==id)EquippedThemeId="paper";
            }
            return changed;
        }
        /// <summary>Transfers reserved refund coins into newly available wallet space, without losing credit.</summary>
        public void ReleaseRetirementCredit()
        {int credit=Math.Min(Math.Max(0,PendingRetirementCoins),EconomyConfig.MaxCoins-Coins);Coins+=credit;PendingRetirementCoins-=credit;}
        /// <summary>Normalizes lists, balance and slots without discarding unknown item ids.</summary>
        public void Sanitize()
        {
            Coins=Math.Max(0,Math.Min(EconomyConfig.MaxCoins,Coins));Trophies=Math.Max(0,Trophies);
            Hints=Math.Max(0,Math.Min(EconomyConfig.MaxBoosts,Hints));Undos=Math.Max(0,Math.Min(EconomyConfig.MaxBoosts,Undos));
            DailyRewardDay=((DailyRewardDay%7)+7)%7;DailyStreak=Math.Max(0,DailyStreak);DailyBestStreak=Math.Max(DailyStreak,DailyBestStreak);DailySolvedCount=Math.Max(0,DailySolvedCount);
            OwnedItemIds=OwnedItemIds??new List<string>(); RewardedStars=RewardedStars??new List<LevelRewardRecord>();
            if(!OwnedItemIds.Contains("classic"))OwnedItemIds.Add("classic");
            if(!OwnedItemIds.Contains("paper"))OwnedItemIds.Add("paper");
            if(!OwnedItemIds.Contains(DefaultAvatarId))OwnedItemIds.Add(DefaultAvatarId);
            if(!OwnedItemIds.Contains(DefaultFrameId))OwnedItemIds.Add(DefaultFrameId);
            if(!OwnedItemIds.Contains(TapSoundSynth.DefaultId))OwnedItemIds.Add(TapSoundSynth.DefaultId);
            if(!ValidSlot(EquippedArrowId,ShopSlot.Arrow))EquippedArrowId="classic";
            if(!ValidSlot(EquippedThemeId,ShopSlot.Theme))EquippedThemeId="paper";
            if(!ValidSlot(EquippedAvatarId,ShopSlot.Avatar))EquippedAvatarId=DefaultAvatarId;
            if(!ValidSlot(EquippedFrameId,ShopSlot.Frame))EquippedFrameId=DefaultFrameId;
            if(!ValidSlot(EquippedSoundId,ShopSlot.Sound))EquippedSoundId=TapSoundSynth.DefaultId;
            JourneyLives=Math.Max(0,Math.Min(JourneyLifeRules.Max,JourneyLives));JourneyRefillTicks=Math.Max(0,JourneyRefillTicks);PlayerName=PlayerNameRules.Clean(PlayerName);
                        NextAttemptId=Math.Max(0,NextAttemptId);LastPaidAttemptId=Math.Max(0,LastPaidAttemptId);
            if(LastPaidAttemptId>0 && NextAttemptId<=LastPaidAttemptId && LastPaidAttemptId<int.MaxValue)NextAttemptId=LastPaidAttemptId+1;
        }
        /// <summary>The equipped id for a slot.</summary>
        public string EquippedIn(ShopSlot slot)=>slot==ShopSlot.Arrow?EquippedArrowId:slot==ShopSlot.Theme?EquippedThemeId:slot==ShopSlot.Avatar?EquippedAvatarId:slot==ShopSlot.Sound?EquippedSoundId:EquippedFrameId;
        /// <summary>Sets the equipped id for a slot without validation.</summary>
        public void SetEquipped(ShopSlot slot,string id){if(slot==ShopSlot.Arrow)EquippedArrowId=id;else if(slot==ShopSlot.Theme)EquippedThemeId=id;else if(slot==ShopSlot.Avatar)EquippedAvatarId=id;else if(slot==ShopSlot.Sound)EquippedSoundId=id;else EquippedFrameId=id;}
        private bool ValidSlot(string id,ShopSlot slot)
        { ShopItem item=ShopCatalog.Find(id);return item!=null&&item.Slot==slot&&OwnedItemIds.Contains(id); }
    }
}
