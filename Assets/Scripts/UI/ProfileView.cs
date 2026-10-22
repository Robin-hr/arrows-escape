using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Neon profile page: progress, stars, coins, collection and equipped items. Read-only.</summary>
    public sealed class ProfileView : MonoBehaviour
    {
        private static readonly Vector4 SecondaryBorder = new Vector4(65, 80, 65, 60);
        private static readonly Vector4 PillBorder = new Vector4(40, 40, 40, 40);
        private Action back;

        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while the page is open.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;
        /// <summary>Returns to the home page.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>"completed / total" levels.</summary>
        public UnityEngine.UI.Text LevelsText { get; private set; }
        /// <summary>"stars / max" stars.</summary>
        public UnityEngine.UI.Text StarsText { get; private set; }
        /// <summary>Levels finished with three stars.</summary>
        public UnityEngine.UI.Text PerfectText { get; private set; }
        /// <summary>Coin balance.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>"owned / total" arrows.</summary>
        public UnityEngine.UI.Text ArrowsText { get; private set; }
        /// <summary>"owned / total" themes.</summary>
        public UnityEngine.UI.Text ThemesText { get; private set; }
        /// <summary>Equipped arrow and theme names.</summary>
        public UnityEngine.UI.Text EquippedText { get; private set; }

        /// <summary>Builds the hidden page.</summary>
        public void Build(Transform parent, Action onBack)
        {
            if (Root != null) return;
            back = onBack;
            Root = UiFactory.ScreenRoot("Profile", parent, BrandArt.Space, out var safe);
            var bg = UiFactory.Rect("ProfileBackground", Root, Vector2.zero, Vector2.zero); UiFactory.Stretch(bg); bg.SetAsFirstSibling();
            bg.gameObject.AddComponent<NeonMenuBackground>().Build(bg);
            var shade = UiFactory.Rect("Shade", bg, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(shade.rectTransform); shade.color = new Color(0.02f, 0.02f, 0.1f, 0.55f); shade.raycastTarget = false;

            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            var backImage = BrandArt.Image("Back", header, "ui_pill", new Vector2(112, 112), new Vector2(-420, -62), PillBorder);
            backImage.raycastTarget = true;
            if (backImage.sprite == null) backImage.color = new Color(0.16f, 0.11f, 0.48f, 0.95f);
            BackButton = backImage.gameObject.AddComponent<UnityEngine.UI.Button>(); BackButton.targetGraphic = backImage;
            backImage.gameObject.AddComponent<ButtonFeel>();
            UiStyle.Icon("BackIcon", BackButton.transform, ArrowEscape.View.UiSpriteShape.Back, Vector2.zero, 46, Color.white);
            BackButton.onClick.AddListener(() => back?.Invoke());
            BrandArt.Text("Title", header, "PROFILE", 64, new Vector2(600, 100), new Vector2(0, -62), Color.white);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }

            var area = UiFactory.Rect("ProfileContent", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(area); area.offsetMax = new Vector2(0, -180);
            var column = UiFactory.Column(area, 900, 1500);
            BrandArt.Image("Avatar", column, "icon_profile", new Vector2(220, 220), new Vector2(0, 590));
            BrandArt.Text("PlayerName", column, "PLAYER", 48, new Vector2(800, 70), new Vector2(0, 440), Color.white);

            LevelsText = Stat(column, "LEVELS", -225, 250);
            StarsText = Stat(column, "STARS", 225, 250);
            PerfectText = Stat(column, "3-STAR LEVELS", -225, -10);
            CoinsText = Stat(column, "COINS", 225, -10);
            ArrowsText = Stat(column, "ARROWS", -225, -270);
            ThemesText = Stat(column, "THEMES", 225, -270);

            var equipped = BrandArt.Image("Equipped", column, "ui_pill", new Vector2(880, 190), new Vector2(0, -520), PillBorder);
            if (equipped.sprite == null) equipped.color = new Color(0.06f, 0.04f, 0.23f, 0.9f);
            BrandArt.Text("EquippedTitle", equipped.transform, "EQUIPPED", 28, new Vector2(800, 44), new Vector2(0, 52), BrandArt.Cyan);
            EquippedText = BrandArt.Text("EquippedNames", equipped.transform, "", 30, new Vector2(820, 100), new Vector2(0, -20), Color.white);
            EquippedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Root.gameObject.SetActive(false);
        }

        private static UnityEngine.UI.Text Stat(Transform parent, string label, float x, float y)
        {
            var card = BrandArt.Image(label, parent, "ui_button_secondary", new Vector2(420, 230), new Vector2(x, y), SecondaryBorder);
            if (card.sprite == null) card.color = new Color(0.16f, 0.11f, 0.48f, 0.95f);
            var value = BrandArt.Text("Value", card.transform, "0", 58, new Vector2(400, 80), new Vector2(0, 30), Color.white);
            BrandArt.Text("Label", card.transform, label, 26, new Vector2(400, 44), new Vector2(0, -52), BrandArt.Lavender);
            return value;
        }

        /// <summary>Fills the page and shows it on top of the menus.</summary>
        public void Show(ProfileStats stats)
        {
            if (Root == null || stats == null) return;
            LevelsText.text = stats.LevelsCompleted + " / " + stats.LevelCount;
            StarsText.text = stats.Stars + " / " + stats.MaxStars;
            PerfectText.text = stats.PerfectLevels.ToString();
            CoinsText.text = stats.Coins.ToString();
            ArrowsText.text = stats.ArrowsOwned + " / " + stats.ArrowsTotal;
            ThemesText.text = stats.ThemesOwned + " / " + stats.ThemesTotal;
            EquippedText.text = "Arrow: " + stats.EquippedArrow + "\nTheme: " + stats.EquippedTheme;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        /// <summary>Hides the page.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
