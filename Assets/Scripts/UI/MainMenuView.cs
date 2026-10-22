using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Neon home page: stars / profile / coins header, the chrome ARROWS ESCAPE logo, a big PLAY button and three tiles.
    /// Public members keep the earlier menu API so navigation code and tests stay unchanged.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        // 9-slice borders (left, bottom, right, top) of the neon frame art in pixels.
        private static readonly Vector4 PrimaryBorder = new Vector4(80, 95, 80, 75);
        private static readonly Vector4 SecondaryBorder = new Vector4(65, 80, 65, 60);
        private static readonly Vector4 PillBorder = new Vector4(40, 40, 40, 40);
        private const float EntranceStagger = 0.06f, EntranceTime = 0.3f;

        private NeonMenuBackground background;
        private CoinCounter coins;
        private RectTransform[] entranceItems;
        private CanvasGroup[] entranceGroups;
        private Vector2[] entranceRest;
        private RectTransform logo;
        private UnityEngine.UI.Image playGlow;
        private float elapsed;
        private string themeId = "paper";

        /// <summary>Menu screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Starts the continue level.</summary>
        public UnityEngine.UI.Button PlayButton { get; private set; }
        /// <summary>"LEVEL N" under PLAY: the level the player continues from.</summary>
        public UnityEngine.UI.Text PlayLevelText { get; private set; }
        /// <summary>Level selection tile.</summary>
        public UnityEngine.UI.Button LevelsButton { get; private set; }
        /// <summary>Shop tile.</summary>
        public UnityEngine.UI.Button ShopButton { get; private set; }
        /// <summary>Settings tile.</summary>
        public UnityEngine.UI.Button SettingsButton { get; private set; }
        /// <summary>Opens the world ranking.</summary>
        public UnityEngine.UI.Button RankingButton { get; private set; }
        /// <summary>Side of the four home tiles.</summary>
        public const float TileSize = 214;
        /// <summary>Profile button in the header.</summary>
        public UnityEngine.UI.Button ProfileButton { get; private set; }
        /// <summary>The player's avatar in their frame, on the profile button.</summary>
        public AvatarBadge Avatar { get; private set; }
        /// <summary>Star counter; opens level select.</summary>
        public UnityEngine.UI.Button StarsButton { get; private set; }
        /// <summary>Retained platform quit API (hidden).</summary>
        public UnityEngine.UI.Button QuitButton { get; private set; }
        /// <summary>Retained completion summary (hidden, still updated).</summary>
        public UnityEngine.UI.Text CompletedText { get; private set; }
        /// <summary>Retained continue subtitle (hidden, still updated).</summary>
        public UnityEngine.UI.Text ContinueText { get; private set; }
        /// <summary>Coin counter; opens the shop.</summary>
        public UnityEngine.UI.Button CoinsButton { get; private set; }
        /// <summary>Visible coin balance.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>Best stars out of the catalog's available stars.</summary>
        public UnityEngine.UI.Text StarsText { get; private set; }
        /// <summary>The home page always uses the neon backdrop; themes only change the play area.</summary>
        public IMenuBackground Background => background;
        /// <summary>Last theme id passed to SetTheme (kept for callers; the home page itself stays neon).</summary>
        public string ThemeId => themeId;

        /// <summary>Builds the runtime screen once.</summary>
        public void Build(Transform parent)
        {
            if (Root != null) return;
            Root = UiFactory.ScreenRoot("MainMenu", parent, BrandArt.Space, out var safe);
            var bg = UiFactory.Rect("MenuBackground", Root, Vector2.zero, Vector2.zero); UiFactory.Stretch(bg); bg.SetAsFirstSibling();
            background = bg.gameObject.AddComponent<NeonMenuBackground>(); background.Build(bg);

            RectTransform header = BuildHeader(safe);

            var area = UiFactory.Rect("MenuContent", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(area); area.offsetMax = new Vector2(0, -180);
            var column = UiFactory.Column(area, 900, 1500);
            logo = UiFactory.Rect("Title", column, new Vector2(900, 441), new Vector2(0, 450));
            var logoImage = logo.gameObject.AddComponent<UnityEngine.UI.RawImage>(); logoImage.texture = BrandArt.Texture("arrows_escape_logo"); logoImage.raycastTarget = false;
            if (logoImage.texture == null) { logoImage.color = new Color(1, 1, 1, 0); BrandArt.Text("TitleText", logo, "ARROWS ESCAPE", 72, new Vector2(900, 120), Vector2.zero, BrandArt.Cyan); }

            playGlow = BrandArt.Image("PlayGlow", column, "ui_button_primary", new Vector2(820, 228), new Vector2(0, -138), PrimaryBorder);
            playGlow.color = new Color(0.31f, 0.85f, 1f, 0.35f);
            NeonKit.Isolate(playGlow.gameObject); NeonKit.Isolate(logo.gameObject);
            PlayButton = NeonButton("Play", column, "ui_button_primary", PrimaryBorder, new Vector2(760, 200), new Vector2(0, -140));
            // The art's lit face sits 11.5 units above the sprite centre (its drop edge is at the bottom); content is centred on the face.
            BrandArt.Image("PlayIcon", PlayButton.transform, "icon_play", new Vector2(120, 120), new Vector2(-250, 12));
            BrandArt.Text("Label", PlayButton.transform, "PLAY", 76, new Vector2(440, 90), new Vector2(60, 36), Color.white);
            PlayLevelText = BrandArt.Text("LevelLabel", PlayButton.transform, "LEVEL 1", 30, new Vector2(440, 44), new Vector2(60, -24), new Color32(230, 247, 255, 255));

            LevelsButton = Tile("Levels", "LEVELS", "icon_levels", column, -342);
            ShopButton = Tile("Shop", "SHOP", "icon_shop", column, -114);
            RankingButton = Tile("Ranking", "RANKING", "icon_ranking", column, 114);
            SettingsButton = Tile("Settings", "SETTINGS", "icon_settings", column, 342);

            CompletedText = UiFactory.Text("Completed", column, "", UiStyle.Caption, new Vector2(700, 64), new Vector2(0, -690)); CompletedText.gameObject.SetActive(false);
            ContinueText = UiFactory.Text("ContinueLevel", column, "", UiStyle.Caption, new Vector2(500, 48), Vector2.zero); ContinueText.gameObject.SetActive(false);
            QuitButton = UiFactory.MenuButton("Quit", column, -720); QuitButton.gameObject.SetActive(false);

            entranceItems = new[] { header, logo, (RectTransform)PlayButton.transform, (RectTransform)LevelsButton.transform, (RectTransform)ShopButton.transform, (RectTransform)RankingButton.transform, (RectTransform)SettingsButton.transform };
            entranceGroups = new CanvasGroup[entranceItems.Length]; entranceRest = new Vector2[entranceItems.Length];
            for (int i = 0; i < entranceItems.Length; i++) { entranceRest[i] = entranceItems[i].anchoredPosition; entranceGroups[i] = entranceItems[i].gameObject.AddComponent<CanvasGroup>(); }
            PlayEntrance();
        }

        private RectTransform BuildHeader(RectTransform safe)
        {
            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));

            StarsButton = NeonButton("Stars", header, "ui_pill", PillBorder, new Vector2(330, 92), new Vector2(-315, -62));
            BrandArt.Image("StarIcon", StarsButton.transform, "icon_star", new Vector2(70, 70), new Vector2(-118, 0));
            StarsText = BrandArt.Text("TotalStars", StarsButton.transform, "0 / 0", 34, new Vector2(230, 60), new Vector2(35, 0), Color.white);

            var profileHit = UiFactory.Rect("Profile", header, new Vector2(140, 140), new Vector2(0, -62)).gameObject.AddComponent<UnityEngine.UI.Image>();
            profileHit.color = new Color(0, 0, 0, 0);
            ProfileButton = profileHit.gameObject.AddComponent<UnityEngine.UI.Button>(); ProfileButton.targetGraphic = profileHit;
            profileHit.gameObject.AddComponent<ButtonFeel>();
            Avatar = AvatarBadge.Create("AvatarBadge", ProfileButton.transform, 140, Vector2.zero);

            CoinsButton = NeonButton("Coins", header, "ui_pill", PillBorder, new Vector2(330, 92), new Vector2(315, -62));
            BrandArt.Image("CoinIcon", CoinsButton.transform, "icon_coin", new Vector2(66, 66), new Vector2(-118, 0));
            CoinsText = BrandArt.Text("CoinsText", CoinsButton.transform, "0", 36, new Vector2(170, 60), new Vector2(10, 0), Color.white);
            BrandArt.Image("PlusBadge", CoinsButton.transform, "icon_plus", new Vector2(50, 50), new Vector2(128, 0));
            coins = CoinsButton.gameObject.AddComponent<CoinCounter>(); coins.Bind(CoinsText);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }
            return header;
        }

        private static UnityEngine.UI.Button NeonButton(string name, Transform parent, string art, Vector4 border, Vector2 size, Vector2 position)
        {
            var image = BrandArt.Image(name, parent, art, size, position, border);
            image.raycastTarget = true;
            if (image.sprite == null) { image.sprite = null; image.color = new Color(0.16f, 0.11f, 0.48f, 0.95f); }
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = Color.white; colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.85f, 0.95f); colors.disabledColor = new Color(0.5f, 0.5f, 0.6f, 0.7f);
            button.colors = colors;
            image.gameObject.AddComponent<ButtonFeel>();
            return button;
        }

        private static UnityEngine.UI.Button Tile(string name, string label, string icon, Transform parent, float x)
        {
            var button = NeonButton(name, parent, "ui_button_secondary", SecondaryBorder, new Vector2(TileSize, TileSize), new Vector2(x, -475));
            // Centred on the tile's lit face so icon and label stay inside the frame (scaled from the 250 design).
            const float k = TileSize / 250f;
            BrandArt.Image("Icon", button.transform, icon, new Vector2(104, 104) * k, new Vector2(0, 34 * k));
            var text = BrandArt.Text("Label", button.transform, label, 24, new Vector2(196, 40), new Vector2(0, -46 * k), BrandArt.Lavender);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = 24;
            return button;
        }

        /// <summary>Restarts the staggered fade/slide entrance (after the startup loader, or when returning).</summary>
        public void PlayEntrance()
        {
            elapsed = 0;
            if (entranceItems == null) return;
            for (int i = 0; i < entranceItems.Length; i++) { entranceGroups[i].alpha = 0; entranceItems[i].anchoredPosition = entranceRest[i] - Vector2.up * 12; }
        }

        /// <summary>Refreshes progress on entering, preserving ContinueIndex navigation.</summary>
        public void Refresh(bool hasProgress, int completed, int total, int continueIndex)
        {
            PlayButton.interactable = total > 0;
            PlayLevelText.text = total > 0 ? "LEVEL " + (continueIndex + 1) : "";
            ContinueText.text = hasProgress && total > 0 ? "Level " + (continueIndex + 1) : "";
            CompletedText.text = $"Completed {completed} / {total}";
            background.Restart(); PlayEntrance();
        }

        /// <summary>Shows the equipped avatar and frame on the profile button.</summary>
        public void SetAvatar(string avatarId, string frameId) { if (Avatar != null && (Avatar.AvatarId != avatarId || Avatar.FrameId != frameId)) Avatar.Set(avatarId, frameId); }

        /// <summary>Remembers the equipped theme; the home page keeps its neon look.</summary>
        public void SetTheme(string id) { themeId = string.IsNullOrEmpty(id) ? "paper" : id; }

        /// <summary>Updates the counters without restarting the entrance.</summary>
        public void SetEconomy(int balance, int totalStars, int maximumStars, bool animate = true)
        {
            if (coins.Value != balance || !animate) coins.SetValue(balance, animate);
            string text = totalStars + " / " + maximumStars;
            if (StarsText.text != text) StarsText.text = text;
        }

        /// <summary>Explicit animation driver for tests and profiling; allocation-free.</summary>
        public void Tick(float dt)
        {
            if (Root == null || !Root.gameObject.activeInHierarchy || !(dt > 0) || float.IsInfinity(dt)) return;
            elapsed += dt;
            background.Tick(dt);
            for (int i = 0; i < entranceItems.Length; i++)
            {
                float t = Mathf.Clamp01((elapsed - i * EntranceStagger) / EntranceTime), ease = 1 - Mathf.Pow(1 - t, 3);
                entranceGroups[i].alpha = ease; entranceItems[i].anchoredPosition = entranceRest[i] - Vector2.up * (12 * (1 - ease));
            }
            float breathe = 1f + 0.01f * (1f - Mathf.Cos(elapsed * Mathf.PI * 2f / 3f));
            logo.localScale = new Vector3(breathe, breathe, 1);
            Color glow = playGlow.color; glow.a = 0.22f + 0.2f * (0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI)); playGlow.color = glow;
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
    }
}
