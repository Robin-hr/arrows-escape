using System;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Daily puzzle page: today's date and status, the fire streak, best streak and the ten streak badges.</summary>
    public sealed class DailyView : MonoBehaviour
    {
        private static readonly Color Locked = new Color(0.35f, 0.33f, 0.5f, 0.45f);
        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;
        /// <summary>Back to home.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Starts (or replays) today's puzzle.</summary>
        public UnityEngine.UI.Button PlayButton { get; private set; }
        /// <summary>Today's date.</summary>
        public UnityEngine.UI.Text DateText { get; private set; }
        /// <summary>Current streak number beside the fire.</summary>
        public UnityEngine.UI.Text StreakText { get; private set; }
        /// <summary>"Best streak N · Solved N".</summary>
        public UnityEngine.UI.Text BestText { get; private set; }
        /// <summary>Solved-today / play-today notice.</summary>
        public UnityEngine.UI.Text StatusText { get; private set; }
        /// <summary>Fire icon (dim when the streak is 0).</summary>
        public UnityEngine.UI.Image Fire { get; private set; }
        /// <summary>The ten streak badges (100 … 1,000 days).</summary>
        public UnityEngine.UI.Image[] Badges { get; private set; }
        /// <summary>Badges earned in the last Show.</summary>
        public int EarnedCount { get; private set; }

        /// <summary>Builds the hidden page.</summary>
        public void Build(Transform parent, Action onPlay, Action onBack)
        {
            Root = UiFactory.ScreenRoot("DailyPuzzle", parent, BrandArt.Space, out var safe);
            NeonKit.Backdrop(Root, 0.55f);
            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            BackButton = NeonKit.IconButton("Back", header, "icon_back", new Vector2(-420, -62), 112, 56);
            BackButton.onClick.AddListener(() => onBack?.Invoke());
            BrandArt.Text("Title", header, "DAILY PUZZLE", 60, new Vector2(700, 100), new Vector2(40, -62), Color.white);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }

            var area = UiFactory.Rect("DailyContent", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(area); area.offsetMax = new Vector2(0, -180);
            var column = UiFactory.Column(area, 940, 1600);
            DateText = BrandArt.Text("Date", column, "", 40, new Vector2(900, 60), new Vector2(0, 740), BrandArt.Cyan);
            var streakCard = UiFactory.Rect("StreakCard", column, new Vector2(900, 420), new Vector2(0, 470)).gameObject.AddComponent<UnityEngine.UI.Image>();
            NeonKit.Panel(streakCard, new Color32(255, 140, 60, 255)); streakCard.raycastTarget = false;
            Fire = NeonKit.Icon("Fire", streakCard.transform, "Daily/icon_fire", 230, new Vector2(-230, 20));
            StreakText = BrandArt.Text("Streak", streakCard.transform, "0", 120, new Vector2(400, 150), new Vector2(150, 50), Color.white);
            BrandArt.Text("StreakLabel", streakCard.transform, "DAY STREAK", 34, new Vector2(400, 60), new Vector2(150, -50), new Color32(255, 196, 120, 255));
            BestText = BrandArt.Text("Best", streakCard.transform, "", 26, new Vector2(840, 50), new Vector2(0, -160), BrandArt.Lavender);
            StatusText = BrandArt.Text("Status", column, "", 30, new Vector2(900, 60), new Vector2(0, 205), Color.white);
            PlayButton = UiFactory.Button("PLAY TODAY'S PUZZLE", column, new Vector2(0, 90), NeonButtonStyle.Primary);
            ((RectTransform)PlayButton.transform).sizeDelta = new Vector2(760, 150); NeonKit.WithIcon(PlayButton, "icon_play", 74);
            PlayButton.onClick.AddListener(() => onPlay?.Invoke());
            BrandArt.Text("BadgesTitle", column, "STREAK BADGES", 34, new Vector2(900, 60), new Vector2(0, -60), BrandArt.Cyan);
            Badges = new UnityEngine.UI.Image[DailyStreakRules.Badges.Length];
            for (int i = 0; i < Badges.Length; i++)
            {
                int row = i / 5, col = i % 5; var pos = new Vector2((col - 2) * 180, -220 - row * 250);
                Badges[i] = BrandArt.Image("Badge" + DailyStreakRules.Badges[i], column, "Daily/badge_streak_" + DailyStreakRules.Badges[i], new Vector2(160, 160), pos);
                BrandArt.Text("Days", Badges[i].transform, DailyStreakRules.Badges[i] + " DAYS", 20, new Vector2(170, 34), new Vector2(0, -102), BrandArt.Lavender);
            }
            Root.gameObject.SetActive(false);
        }

        /// <summary>Fills and opens the page.</summary>
        public void Show(Inventory inventory, int todayKey)
        {
            if (Root == null) return;
            int streak = DailyStreakRules.CurrentStreak(inventory, todayKey); bool solved = DailyStreakRules.SolvedToday(inventory, todayKey);
            DateText.text = DailyCalendar.Pretty(todayKey).ToUpperInvariant();
            StreakText.text = streak.ToString();
            Fire.sprite = BrandArt.SpriteOf(streak > 0 ? "Daily/icon_fire" : "Daily/icon_fire_dim");
            int next = DailyStreakRules.NextBadge(inventory.DailyBestStreak);
            BestText.text = "Best " + inventory.DailyBestStreak + "  ·  Solved " + inventory.DailySolvedCount + (next > 0 ? "  ·  Next badge " + next + " days" : "  ·  All badges earned!");
            StatusText.text = solved ? "Solved today! Come back tomorrow to grow your streak." : streak > 0 ? "Solve today's puzzle to keep your streak alive!" : "Solve a puzzle every day to build a streak.";
            PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = solved ? "PLAY AGAIN" : "PLAY TODAY'S PUZZLE";
            EarnedCount = DailyStreakRules.EarnedBadges(inventory.DailyBestStreak);
            for (int i = 0; i < Badges.Length; i++) Badges[i].color = i < EarnedCount ? Color.white : Locked;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        /// <summary>Hides the page.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
