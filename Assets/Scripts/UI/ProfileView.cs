using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Neon profile page: progress, stars, coins, collection and equipped items. Read-only.</summary>
    public sealed class ProfileView : MonoBehaviour
    {
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
        /// <summary>"owned / total" avatars.</summary>
        public UnityEngine.UI.Text AvatarsText { get; private set; }
        /// <summary>"owned / total" avatar frames.</summary>
        public UnityEngine.UI.Text FramesText { get; private set; }
        /// <summary>The equipped avatar in its frame; tapping it opens the Avatars shop.</summary>
        public AvatarBadge Avatar { get; private set; }
        /// <summary>Opens the shop on the Avatars tab.</summary>
        public UnityEngine.UI.Button AvatarButton { get; private set; }
        /// <summary>Equipped arrow and theme names.</summary>
        public UnityEngine.UI.Text EquippedText { get; private set; }

        /// <summary>Builds the hidden page.</summary>
        public void Build(Transform parent, Action onBack, Action onAvatar = null)
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
            var hit = UiFactory.Rect("AvatarButton", column, new Vector2(300, 300), new Vector2(0, 590)).gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = new Color(0, 0, 0, 0);
            AvatarButton = hit.gameObject.AddComponent<UnityEngine.UI.Button>(); AvatarButton.targetGraphic = hit; hit.gameObject.AddComponent<ButtonFeel>();
            Avatar = AvatarBadge.Create("AvatarBadge", AvatarButton.transform, 300, Vector2.zero);
            NeonKit.Icon("EditBadge", AvatarButton.transform, "icon_plus", 64, new Vector2(104, -104));
            AvatarButton.onClick.AddListener(() => onAvatar?.Invoke());
            BrandArt.Text("PlayerName", column, "PLAYER", 46, new Vector2(800, 64), new Vector2(0, 410), Color.white);

            LevelsText = Stat(column, "LEVELS", -225, 270);
            StarsText = Stat(column, "STARS", 225, 270);
            PerfectText = Stat(column, "3-STAR LEVELS", -225, 70);
            CoinsText = Stat(column, "COINS", 225, 70);
            ArrowsText = Stat(column, "ARROWS", -225, -130);
            ThemesText = Stat(column, "THEMES", 225, -130);
            AvatarsText = Stat(column, "AVATARS", -225, -330);
            FramesText = Stat(column, "FRAMES", 225, -330);

            var equipped = UiFactory.Rect("Equipped", column, new Vector2(900, 210), new Vector2(0, -575)).gameObject.AddComponent<UnityEngine.UI.Image>();
            equipped.raycastTarget = false; NeonKit.Panel(equipped, NeonKit.Cyan);
            BrandArt.Text("EquippedTitle", equipped.transform, "EQUIPPED", 28, new Vector2(800, 44), new Vector2(0, 62), BrandArt.Cyan);
            EquippedText = BrandArt.Text("EquippedNames", equipped.transform, "", 26, new Vector2(820, 120), new Vector2(0, -18), Color.white);
            EquippedText.horizontalOverflow = HorizontalWrapMode.Wrap; EquippedText.lineSpacing = 1.15f;
            Root.gameObject.SetActive(false);
        }

        private static UnityEngine.UI.Text Stat(Transform parent, string label, float x, float y)
        {
            var card = UiFactory.Rect(label, parent, new Vector2(420, 180), new Vector2(x, y)).gameObject.AddComponent<UnityEngine.UI.Image>();
            card.raycastTarget = false; NeonKit.Panel(card, NeonKit.Cyan);
            var value = BrandArt.Text("Value", card.transform, "0", 52, new Vector2(400, 70), new Vector2(0, 18), Color.white);
            BrandArt.Text("Label", card.transform, label, 24, new Vector2(400, 40), new Vector2(0, -40), BrandArt.Lavender);
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
            AvatarsText.text = stats.AvatarsOwned + " / " + stats.AvatarsTotal;
            FramesText.text = stats.FramesOwned + " / " + stats.FramesTotal;
            EquippedText.text = "Arrow: " + stats.EquippedArrow + "   Theme: " + stats.EquippedTheme + "\nAvatar: " + stats.EquippedAvatar + "   Frame: " + stats.EquippedFrame;
            Avatar.Set(stats.AvatarId, stats.FrameId);
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        /// <summary>Hides the page.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
