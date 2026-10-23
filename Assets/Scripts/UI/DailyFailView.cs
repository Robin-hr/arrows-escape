using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Shown on any level (and the daily puzzle) when all three hearts are lost: watch an ad for one heart, buy one
    /// for <see cref="EconomyConfig.HeartPrice"/> coins, restart, or go home.
    /// </summary>
    public sealed class DailyFailView : MonoBehaviour
    {
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Restarts the attempt with three hearts.</summary>
        public UnityEngine.UI.Button RestartButton { get; private set; }
        /// <summary>Watches an ad and continues with one heart.</summary>
        public UnityEngine.UI.Button AdButton { get; private set; }
        /// <summary>Buys one heart for coins and continues.</summary>
        public UnityEngine.UI.Button BuyButton { get; private set; }
        /// <summary>Leaves to the home page.</summary>
        public UnityEngine.UI.Button HomeButton { get; private set; }
        private RectTransform card;
        private float popTime = 1;
        /// <summary>Builds the hidden panel.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("HeartsFail", parent, ViewTheme.PanelDim, out var safe);
            card = UiFactory.Column(safe, 880, 1140);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Magenta);
            BrandArt.Text("Title", card, "OUT OF HEARTS", 60, new Vector2(820, 100), new Vector2(0, 460), Color.white);
            for (int i = 0; i < 3; i++) BrandArt.Image("Heart" + i, card, "Daily/icon_heart_empty", new Vector2(110, 110), new Vector2((i - 1) * 130, 320));
            BrandArt.Text("Note", card, "Three mistakes used up your hearts.\nGet a heart to keep going.", 30, new Vector2(800, 110), new Vector2(0, 180), BrandArt.Lavender);
            AdButton = UiFactory.Button("WATCH AD · +1 HEART", card, new Vector2(0, 40), NeonButtonStyle.Primary);
            ((RectTransform)AdButton.transform).sizeDelta = new Vector2(720, 140); NeonKit.WithIcon(AdButton, "Daily/icon_ad", 70);
            BuyButton = UiFactory.Button("BUY HEART · " + EconomyConfig.HeartPrice, card, new Vector2(0, -120));
            ((RectTransform)BuyButton.transform).sizeDelta = new Vector2(720, 140); NeonKit.WithIcon(BuyButton, "icon_coin", 66);
            RestartButton = UiFactory.Button("RESTART", card, new Vector2(-185, -300));
            ((RectTransform)RestartButton.transform).sizeDelta = new Vector2(350, 120); NeonKit.WithIcon(RestartButton, "icon_restart", 54);
            HomeButton = UiFactory.Button("HOME", card, new Vector2(185, -300));
            ((RectTransform)HomeButton.transform).sizeDelta = new Vector2(350, 120); NeonKit.WithIcon(HomeButton, "icon_home", 54);
            Root.gameObject.SetActive(false);
        }
        /// <summary>Opens the panel; the buy option shows how many coins are missing.</summary>
        public void Show(int coins)
        {
            bool afford = coins >= EconomyConfig.HeartPrice;
            BuyButton.interactable = afford;
            BuyButton.GetComponentInChildren<UnityEngine.UI.Text>().text = afford ? "BUY HEART · " + EconomyConfig.HeartPrice : "NEED " + (EconomyConfig.HeartPrice - coins) + " MORE COINS";
            Root.gameObject.SetActive(true); Root.SetAsLastSibling(); popTime = 0; card.localScale = Vector3.one * (Application.isPlaying ? 0.6f : 1f);
        }
        /// <summary>Closes the panel.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
        private void Update()
        {
            if (!Application.isPlaying || !IsOpen || popTime >= 1) return;
            popTime = Mathf.Min(1, popTime + Time.unscaledDeltaTime / 0.28f);
            card.localScale = Vector3.one * Motion.BackOut(0.6f, 1f, popTime);
        }
    }
}
