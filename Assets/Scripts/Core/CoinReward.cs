using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure reward arithmetic; inventory receipts handle duplicate delivery separately.</summary>
    public static class CoinReward
    {
        /// <summary>Computes first-clear or replay rewards from inventory's rewarded stars.</summary>
        public static int Compute(bool hasRewardRecord, int rewardedStars, int newStars)
        {
            if (rewardedStars < 0 || rewardedStars > 3) throw new ArgumentOutOfRangeException(nameof(rewardedStars));
            if (newStars < 0 || newStars > 3) throw new ArgumentOutOfRangeException(nameof(newStars));
            return hasRewardRecord ? EconomyConfig.ReplayClearCoins + EconomyConfig.CoinsPerNewStar * Math.Max(0, newStars - rewardedStars)
                : EconomyConfig.FirstClearCoins + EconomyConfig.CoinsPerNewStar * newStars;
        }
    }
}
