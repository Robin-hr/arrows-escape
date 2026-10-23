using System;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Stand-in rewarded ad: a full-screen "ad" card with a short countdown, then the reward. Instant outside
    /// Play mode so Edit Mode tests run synchronously. Replace by registering a real network in RewardedAds.Service.
    /// </summary>
    public sealed class AdPlaceholderView : MonoBehaviour, IRewardedAds
    {
        /// <summary>Seconds the placeholder ad runs.</summary>
        public const float Seconds = 3f;
        private Action<bool> pending;
        private float remaining;
        private UnityEngine.UI.Text countdown;
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while the placeholder ad is on screen.</summary>
        public bool IsShowing => Root != null && Root.gameObject.activeSelf;
        /// <inheritdoc/>
        public bool IsReady => true;
        /// <summary>Builds the hidden overlay.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("RewardedAd", parent, new Color(0.01f, 0.01f, 0.05f, 0.97f), out var safe);
            var card = UiFactory.Column(safe, 860, 760);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Gold);
            NeonKit.Icon("AdIcon", card, "Daily/icon_ad", 200, new Vector2(0, 170));
            BrandArt.Text("Title", card, "REWARDED AD", 56, new Vector2(800, 90), new Vector2(0, 0), Color.white);
            BrandArt.Text("Note", card, "Your reward arrives when the ad ends", 28, new Vector2(800, 60), new Vector2(0, -80), BrandArt.Lavender);
            countdown = BrandArt.Text("Countdown", card, "3", 90, new Vector2(300, 130), new Vector2(0, -220), NeonKit.Gold);
            Root.gameObject.SetActive(false);
        }
        /// <inheritdoc/>
        public void Show(string placement, Action<bool> completed)
        {
            if (!Application.isPlaying || Root == null) { completed?.Invoke(true); return; }
            pending = completed; remaining = Seconds; countdown.text = "3";
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        /// <summary>Advances the countdown; grants the reward at the end.</summary>
        public void Tick(float dt)
        {
            if (!IsShowing || !(dt > 0)) return;
            remaining -= dt; countdown.text = Mathf.Max(1, Mathf.CeilToInt(remaining)).ToString();
            if (remaining > 0) return;
            Root.gameObject.SetActive(false); var done = pending; pending = null; done?.Invoke(true);
        }
        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }
    }
}
