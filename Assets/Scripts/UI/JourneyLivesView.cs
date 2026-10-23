using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// "NO LIVES LEFT" for the Journey: shows the five life slots and the refill countdown, and offers an ad for one
    /// life or one life for <see cref="EconomyConfig.JourneyLifePrice"/> coins.
    /// </summary>
    public sealed class JourneyLivesView : MonoBehaviour
    {
        private RectTransform card;
        private readonly UnityEngine.UI.Image[] hearts = new UnityEngine.UI.Image[JourneyLifeRules.Max];
        private Func<Inventory> inventory;
        private Func<long> now;
        private float popTime = 1, refresh;
        private int shownSeconds = -1;
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Watches an ad for one life.</summary>
        public UnityEngine.UI.Button AdButton { get; private set; }
        /// <summary>Buys one life.</summary>
        public UnityEngine.UI.Button BuyButton { get; private set; }
        /// <summary>Closes the panel.</summary>
        public UnityEngine.UI.Button CloseButton { get; private set; }
        /// <summary>"Next life in mm:ss".</summary>
        public UnityEngine.UI.Text TimerText { get; private set; }

        /// <summary>Builds the hidden panel.</summary>
        public void Build(Transform parent, Func<Inventory> state, Func<long> clock)
        {
            inventory = state; now = clock;
            Root = UiFactory.ScreenRoot("JourneyLives", parent, ViewTheme.PanelDim, out var safe);
            card = UiFactory.Column(safe, 880, 1080);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Magenta);
            BrandArt.Text("Title", card, "NO LIVES LEFT", 60, new Vector2(820, 100), new Vector2(0, 430), Color.white);
            for (int i = 0; i < hearts.Length; i++) hearts[i] = BrandArt.Image("Life" + i, card, "Daily/icon_heart_empty", new Vector2(110, 110), new Vector2((i - 2) * 130, 290));
            TimerText = BrandArt.Text("Timer", card, "", 34, new Vector2(800, 60), new Vector2(0, 170), BrandArt.Cyan);
            BrandArt.Text("Note", card, "Every Journey level costs one life.\nLives refill over time, even when the game is closed.", 26, new Vector2(820, 90), new Vector2(0, 90), BrandArt.Lavender);
            AdButton = UiFactory.Button("WATCH AD · +1 LIFE", card, new Vector2(0, -50), NeonButtonStyle.Primary);
            ((RectTransform)AdButton.transform).sizeDelta = new Vector2(720, 140); NeonKit.WithIcon(AdButton, "Daily/icon_ad", 70);
            BuyButton = UiFactory.Button("BUY 1 LIFE · " + EconomyConfig.JourneyLifePrice, card, new Vector2(0, -210));
            ((RectTransform)BuyButton.transform).sizeDelta = new Vector2(720, 140); NeonKit.WithIcon(BuyButton, "icon_coin", 66);
            CloseButton = UiFactory.Button("CLOSE", card, new Vector2(0, -380));
            ((RectTransform)CloseButton.transform).sizeDelta = new Vector2(420, 120);
            CloseButton.onClick.AddListener(Hide);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Opens the panel.</summary>
        public void Show()
        {
            Root.gameObject.SetActive(true); Root.SetAsLastSibling(); popTime = 0; card.localScale = Vector3.one * (Application.isPlaying ? 0.6f : 1f);
            shownSeconds = -1; Refresh();
        }

        /// <summary>Updates hearts, timer and the buy option.</summary>
        public void Refresh()
        {
            if (!IsOpen || inventory == null) return;
            var state = inventory(); long t = now();
            int lives = JourneyLifeRules.Update(state, t), seconds = JourneyLifeRules.SecondsToNext(state, t);
            for (int i = 0; i < hearts.Length; i++) hearts[i].sprite = BrandArt.SpriteOf(i < lives ? "Daily/icon_heart" : "Daily/icon_heart_empty");
            if (seconds != shownSeconds) { shownSeconds = seconds; TimerText.text = lives >= JourneyLifeRules.Max ? "LIVES FULL" : "NEXT LIFE IN " + JourneyLifeRules.Format(seconds); }
            bool afford = state.Coins >= EconomyConfig.JourneyLifePrice, room = lives < JourneyLifeRules.Max;
            BuyButton.interactable = afford && room; AdButton.interactable = room;
        }

        /// <summary>Closes the panel.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }

        private void Update()
        {
            if (!Application.isPlaying || !IsOpen) return;
            refresh += Time.unscaledDeltaTime; if (refresh >= 0.25f) { refresh = 0; Refresh(); }
            if (popTime >= 1) return;
            popTime = Mathf.Min(1, popTime + Time.unscaledDeltaTime / 0.28f);
            card.localScale = Vector3.one * Motion.BackOut(0.6f, 1f, popTime);
        }
    }
}
