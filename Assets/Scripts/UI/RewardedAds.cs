namespace ArrowEscape.UI
{
    /// <summary>
    /// The active rewarded-ad provider. Until a real ad network (e.g. Unity LevelPlay) is registered, the
    /// <see cref="AdPlaceholderView"/> stands in so every "watch an ad" flow works end to end.
    /// </summary>
    public static class RewardedAds
    {
        /// <summary>Current provider; AppFlowController installs the placeholder when none is set.</summary>
        public static IRewardedAds Service { get; set; }
    }
}
