using System;
namespace ArrowEscape.UI
{
    /// <summary>Rewarded video ads. The callback receives true only when the reward was earned.</summary>
    public interface IRewardedAds
    {
        /// <summary>True when an ad can be shown now.</summary>
        bool IsReady { get; }
        /// <summary>Shows an ad for a placement ("hint", "undo", "daily_continue").</summary>
        void Show(string placement, Action<bool> completed);
    }
}
