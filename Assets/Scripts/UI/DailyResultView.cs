using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Daily puzzle complete: result, fire streak, any new streak badge, Share (copy) and Home.</summary>
    public sealed class DailyResultView : MonoBehaviour
    {
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Copies the result text to the clipboard.</summary>
        public UnityEngine.UI.Button ShareButton { get; private set; }
        /// <summary>Back to home.</summary>
        public UnityEngine.UI.Button HomeButton { get; private set; }
        /// <summary>Date line.</summary>
        public UnityEngine.UI.Text DateText { get; private set; }
        /// <summary>Moves and mistakes.</summary>
        public UnityEngine.UI.Text StatsText { get; private set; }
        /// <summary>Streak number.</summary>
        public UnityEngine.UI.Text StreakText { get; private set; }
        /// <summary>New badge banner (hidden unless earned now).</summary>
        public UnityEngine.UI.Image NewBadge { get; private set; }
        private UnityEngine.UI.Image[] hearts;
        private UnityEngine.UI.Text badgeText;
        /// <summary>Builds the hidden panel.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("DailyResult", parent, ViewTheme.PanelDim, out var safe);
            var card = UiFactory.Column(safe, 900, 1240);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Gold);
            BrandArt.Text("Title", card, "DAILY COMPLETE!", 62, new Vector2(860, 100), new Vector2(0, 530), new Color32(255, 214, 92, 255));
            DateText = BrandArt.Text("Date", card, "", 32, new Vector2(860, 50), new Vector2(0, 455), BrandArt.Cyan);
            NeonKit.Icon("Fire", card, "Daily/icon_fire", 210, new Vector2(-170, 290));
            StreakText = BrandArt.Text("Streak", card, "1", 110, new Vector2(320, 140), new Vector2(120, 320), Color.white);
            BrandArt.Text("StreakLabel", card, "DAY STREAK", 32, new Vector2(320, 50), new Vector2(120, 225), new Color32(255, 196, 120, 255));
            hearts = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++) hearts[i] = BrandArt.Image("Heart" + i, card, "Daily/icon_heart", new Vector2(90, 90), new Vector2((i - 1) * 110, 110));
            StatsText = BrandArt.Text("Stats", card, "", 36, new Vector2(860, 60), new Vector2(0, 20), Color.white);
            NewBadge = BrandArt.Image("NewBadge", card, "Daily/badge_streak_100", new Vector2(190, 190), new Vector2(-250, -150));
            badgeText = BrandArt.Text("BadgeText", card, "", 32, new Vector2(520, 120), new Vector2(110, -150), new Color32(255, 214, 92, 255));
            badgeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ShareButton = UiFactory.Button("SHARE RESULT", card, new Vector2(0, -350), NeonButtonStyle.Primary);
            ((RectTransform)ShareButton.transform).sizeDelta = new Vector2(680, 140); NeonKit.WithIcon(ShareButton, "Daily/icon_share", 66);
            HomeButton = UiFactory.Button("HOME", card, new Vector2(0, -505));
            ((RectTransform)HomeButton.transform).sizeDelta = new Vector2(440, 120); NeonKit.WithIcon(HomeButton, "icon_home", 56);
            Root.gameObject.SetActive(false);
        }
        /// <summary>Fills and opens the panel.</summary>
        public void Show(int dateKey, int moves, int mistakes, int heartsLeft, int streak, int badgeEarned, int nextBadge)
        {
            DateText.text = DailyCalendar.Pretty(dateKey).ToUpperInvariant();
            StreakText.text = streak.ToString();
            StatsText.text = moves + " moves  ·  " + mistakes + (mistakes == 1 ? " mistake" : " mistakes");
            for (int i = 0; i < hearts.Length; i++) hearts[i].sprite = BrandArt.SpriteOf(i < heartsLeft ? "Daily/icon_heart" : "Daily/icon_heart_empty");
            bool earned = badgeEarned > 0;
            NewBadge.gameObject.SetActive(earned);
            if (earned) { NewBadge.sprite = BrandArt.SpriteOf("Daily/badge_streak_" + badgeEarned); badgeText.text = "NEW BADGE!\n" + badgeEarned + "-DAY STREAK"; badgeText.rectTransform.anchoredPosition = new Vector2(110, -150); }
            else { badgeText.text = nextBadge > 0 ? (nextBadge - streak) + " more days to the " + nextBadge + "-day badge" : "Every streak badge earned!"; badgeText.rectTransform.anchoredPosition = new Vector2(0, -150); }
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        /// <summary>Closes the panel.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
