using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Journey: every level as a stop on one winding neon road, level 1 at the bottom. Only the visible stretch is
    /// drawn (a pooled set of nodes, decorations and one road mesh), so all 360 levels scroll smoothly at 60 fps.
    /// The player's avatar marks the current level; floating 3D crystals and world banners decorate the way.
    /// </summary>
    public sealed class JourneyView : MonoBehaviour
    {
        private const int NodePool = 16, DecorPool = 14, BannerPool = 2;
        private const float DecorStep = 1.6f;
        private readonly List<JourneyNode> nodes = new List<JourneyNode>();
        private readonly UnityEngine.UI.Image[] decor = new UnityEngine.UI.Image[DecorPool];
        private readonly int[] decorSlot = new int[DecorPool];
        private readonly Vector2[] decorBase = new Vector2[DecorPool];
        private readonly RectTransform[] banners = new RectTransform[BannerPool];
        private readonly UnityEngine.UI.Text[] bannerTexts = new UnityEngine.UI.Text[BannerPool];
        private readonly int[] bannerWorld = new int[BannerPool];
        private readonly TapStreak lockedTaps = new TapStreak(3, 0.6f);
        private int lockedIndex = -1;
        private IReadOnlyList<LevelTile> tiles;
        private Action back;
        private Action<int> play;
        private RectTransform content, viewport, marker, halo;
        private JourneyRoad road;
        private int current, shownFirst = -1, shownLast = -1;
        private float clock;

        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;
        /// <summary>Back to the home page.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Plays the current level.</summary>
        public UnityEngine.UI.Button PlayButton { get; private set; }
        /// <summary>Label on <see cref="PlayButton"/>.</summary>
        public UnityEngine.UI.Text PlayText { get; private set; }
        /// <summary>The scrolling map.</summary>
        public UnityEngine.UI.ScrollRect Scroll { get; private set; }
        /// <summary>The player's avatar above the current level.</summary>
        public AvatarBadge Avatar { get; private set; }
        /// <summary>The player's name under the marker avatar.</summary>
        public UnityEngine.UI.Text NameText { get; private set; }
        /// <summary>Journey lives pill (opens the lives panel).</summary>
        public UnityEngine.UI.Button LivesButton { get; private set; }
        /// <summary>Lives left.</summary>
        public UnityEngine.UI.Text LivesText { get; private set; }
        /// <summary>Refill countdown or FULL.</summary>
        public UnityEngine.UI.Text LivesTimerText { get; private set; }
        /// <summary>Supplies the lives shown in the header: (lives, seconds to the next life).</summary>
        public Func<Vector2Int> LivesSource { get; set; }
        private int shownLives = -1, shownSeconds = -1;
        private float livesClock;
        /// <summary>Refreshes the lives pill now.</summary>
        public void RefreshLives()
        {
            if (LivesSource == null) return;
            Vector2Int v = LivesSource(); if (v.x == shownLives && v.y == shownSeconds) return;
            shownLives = v.x; shownSeconds = v.y;
            LivesText.text = v.x.ToString(); LivesTimerText.text = v.x >= JourneyLifeRules.Max ? "FULL" : JourneyLifeRules.Format(v.y);
        }
        /// <summary>Road mesh.</summary>
        public JourneyRoad Road => road;
        /// <summary>Pooled level nodes (inactive ones are unused).</summary>
        public IReadOnlyList<JourneyNode> Nodes => nodes;
        /// <summary>The level the marker stands on.</summary>
        public int CurrentIndex => current;
        /// <summary>Developer option: three quick taps on a locked level open it (null = off).</summary>
        public Action<int> DeveloperOpen { get; set; }
        /// <summary>Clock for the developer triple tap; tests may replace it.</summary>
        public Func<float> Clock { get; set; } = () => Time.realtimeSinceStartup;

        /// <summary>Builds the hidden page.</summary>
        public void Build(Transform parent, Action<int> onPlay, Action onBack)
        {
            if (Root != null) return;
            play = onPlay; back = onBack;
            Root = UiFactory.ScreenRoot("Journey", parent, BrandArt.Space, out var safe);
            NeonKit.Backdrop(Root, 0.42f);

            var view = UiFactory.Rect("Map", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(view);
            Scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            viewport = UiFactory.Rect("Viewport", view, Vector2.zero, Vector2.zero); UiFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hit = viewport.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = new Color(0, 0, 0, 0);
            content = UiFactory.Rect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(1, 0); content.pivot = new Vector2(0.5f, 0);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 2000);
            Scroll.viewport = viewport; Scroll.content = content; Scroll.horizontal = false; Scroll.vertical = true;
            Scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic; Scroll.scrollSensitivity = 60; Scroll.decelerationRate = 0.12f;

            for (int i = 0; i < DecorPool; i++)
            {
                decor[i] = BrandArt.Image("Crystal" + i, content, "Journey/jn_glow", new Vector2(100, 100), Vector2.zero);
                decor[i].rectTransform.anchorMin = decor[i].rectTransform.anchorMax = new Vector2(0.5f, 0); decor[i].preserveAspect = true;
                decorSlot[i] = int.MinValue; decor[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < BannerPool; i++)
            {
                var panel = UiFactory.Rect("World" + i, content, new Vector2(360, 116), Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
                panel.raycastTarget = false; GlassKit.Apply(panel, GlassKit.Kind.Pill, new Color32(120, 90, 255, 255));
                banners[i] = panel.rectTransform; banners[i].anchorMin = banners[i].anchorMax = new Vector2(0.5f, 0);
                bannerTexts[i] = BrandArt.Text("Name", banners[i], "", 26, new Vector2(330, 90), new Vector2(0, 4), Color.white);
                bannerTexts[i].alignment = TextAnchor.MiddleCenter;
                bannerWorld[i] = -1; banners[i].gameObject.SetActive(false);
            }
            var roadRect = UiFactory.Rect("Road", content, Vector2.zero, Vector2.zero); UiFactory.Stretch(roadRect); roadRect.pivot = new Vector2(0.5f, 0);
            road = roadRect.gameObject.AddComponent<JourneyRoad>(); road.raycastTarget = false;

            halo = BrandArt.Image("CurrentGlow", content, "Journey/jn_glow", new Vector2(300, 300), Vector2.zero).rectTransform;
            halo.anchorMin = halo.anchorMax = new Vector2(0.5f, 0); halo.GetComponent<UnityEngine.UI.Image>().color = new Color32(255, 95, 210, 150);
            for (int i = 0; i < NodePool; i++) nodes.Add(CreateNode(i));
            marker = UiFactory.Rect("Marker", content, new Vector2(120, 120), Vector2.zero);
            marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0);
            var pin = BrandArt.Image("Pin", marker, "Glass/avatar_glow", new Vector2(170, 170), new Vector2(0, 6)); pin.color = new Color32(56, 225, 255, 170);
            Avatar = AvatarBadge.Create("Avatar", marker, 120, Vector2.zero);
            AvatarPulse.Attach(Avatar);
            NameText = BrandArt.NameText("Name", marker, 34, new Vector2(320, 50), new Vector2(0, -86));

            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            var plate = UiFactory.Rect("Plate", header, new Vector2(500, 110), new Vector2(-70, -62)).gameObject.AddComponent<UnityEngine.UI.Image>();
            plate.raycastTarget = false; GlassKit.Apply(plate, GlassKit.Kind.Pill, new Color32(70, 60, 200, 255));
            BackButton = NeonKit.IconButton("Back", header, "icon_back", new Vector2(-420, -62), 112, 56);
            BackButton.onClick.AddListener(() => back?.Invoke());
            BrandArt.Text("Title", header, "JOURNEY", 52, new Vector2(460, 100), new Vector2(-70, -60), Color.white);
            var livesImage = UiFactory.Rect("Lives", header, new Vector2(250, 110), new Vector2(345, -62)).gameObject.AddComponent<UnityEngine.UI.Image>();
            GlassKit.Apply(livesImage, GlassKit.Kind.Pill, new Color32(255, 70, 120, 255));
            LivesButton = livesImage.gameObject.AddComponent<UnityEngine.UI.Button>(); LivesButton.targetGraphic = livesImage; livesImage.gameObject.AddComponent<ButtonFeel>();
            BrandArt.Image("Heart", livesImage.transform, "Daily/icon_heart", new Vector2(70, 70), new Vector2(-72, 4));
            LivesText = BrandArt.Text("Count", livesImage.transform, "5", 40, new Vector2(80, 50), new Vector2(-72, 2), Color.white);
            LivesTimerText = BrandArt.Text("Timer", livesImage.transform, "FULL", 26, new Vector2(140, 40), new Vector2(42, 4), Color.white);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }

            var foot = UiFactory.Rect("Footer", safe, new Vector2(600, 150), Vector2.zero);
            foot.anchorMin = foot.anchorMax = foot.pivot = new Vector2(0.5f, 0); foot.anchoredPosition = new Vector2(0, 36);
            var playImage = UiFactory.Rect("Play", foot, new Vector2(560, 132), Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            GlassKit.Apply(playImage, GlassKit.Kind.Tile, new Color32(60, 230, 150, 255));
            PlayButton = playImage.gameObject.AddComponent<UnityEngine.UI.Button>(); PlayButton.targetGraphic = playImage; playImage.gameObject.AddComponent<ButtonFeel>();
            BrandArt.Image("Icon", playImage.transform, "icon_play", new Vector2(76, 76), new Vector2(-190, 6));
            PlayText = BrandArt.Text("Label", playImage.transform, "PLAY LEVEL 1", 38, new Vector2(400, 70), new Vector2(40, 6), Color.white);
            PlayButton.onClick.AddListener(() => Play(current));
            Root.gameObject.SetActive(false);
        }

        private JourneyNode CreateNode(int slot)
        {
            var node = JourneyNode.Create(content, slot);
            node.Button.onClick.AddListener(() => Play(node.Index));
            node.LockedTap.Tapped += () => { if (!node.Button.interactable) TapLocked(node.Index); };
            return node;
        }

        /// <summary>Opens the map at the player's current level.</summary>
        public void Show(IReadOnlyList<LevelTile> levels, int currentIndex, string avatarId, string frameId, string playerName = null)
        {
            if (Root == null) return;
            tiles = levels ?? new List<LevelTile>();
            current = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, tiles.Count - 1));
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            Avatar.Set(avatarId, frameId); NameText.text = PlayerNameRules.Display(playerName);
            shownLives = shownSeconds = -1; RefreshLives();
            content.sizeDelta = new Vector2(0, JourneyLayout.Height(tiles.Count));
            PlayText.text = "PLAY LEVEL " + (current + 1);
            PlayButton.interactable = tiles.Count > 0;
            shownFirst = shownLast = -1; for (int i = 0; i < BannerPool; i++) bannerWorld[i] = -1; for (int i = 0; i < DecorPool; i++) decorSlot[i] = int.MinValue;
            Canvas.ForceUpdateCanvases();
            FocusCurrent();
            Refresh();
            clock = 0; Tick(0);
        }

        /// <summary>Scrolls so the current level sits in the middle of the screen.</summary>
        public void FocusCurrent()
        {
            float view = viewport.rect.height, total = content.sizeDelta.y;
            if (total <= view || view <= 0) { Scroll.verticalNormalizedPosition = 0; return; }
            Scroll.StopMovement();
            Scroll.verticalNormalizedPosition = Mathf.Clamp01((JourneyLayout.Y(current) - view * 0.45f) / (total - view));
        }

        /// <summary>Visible map heights (bottom, top) in map units.</summary>
        public Vector2 VisibleSpan
        {
            get { float low = -content.anchoredPosition.y; return new Vector2(low, low + viewport.rect.height); }
        }

        /// <summary>Rebinds pooled nodes, decorations and the road for the visible stretch (cheap when unchanged).</summary>
        public void Refresh()
        {
            if (tiles == null || tiles.Count == 0) { foreach (var n in nodes) n.Bind(null, false); return; }
            Vector2 span = VisibleSpan;
            JourneyLayout.VisibleRange(span.x, span.y, tiles.Count, out int first, out int last);
            if (first == shownFirst && last == shownLast) return;
            shownFirst = first; shownLast = last;
            int slot = 0;
            for (int i = first; i <= last && slot < nodes.Count; i++, slot++) nodes[slot].Bind(tiles[i], i == current);
            for (; slot < nodes.Count; slot++) nodes[slot].Bind(null, false);
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].Index == current) nodes[i].transform.SetAsLastSibling();
            marker.SetAsLastSibling();
            road.Set(tiles.Count, first - 1.2f, last + 1.2f, current);
            PlaceBanners(first, last);
            PlaceDecor(first, last);
        }

        private void PlaceBanners(int first, int last)
        {
            int used = 0;
            for (int w = first / JourneyLayout.WorldSize; w <= last / JourneyLayout.WorldSize + 1 && used < BannerPool; w++)
            {
                int start = w * JourneyLayout.WorldSize; if (start < first - 2 || start > last + 2 || start >= tiles.Count) continue;
                float t = start - 0.5f; float side = JourneyLayout.X(t) >= 0 ? -1 : 1;
                banners[used].anchoredPosition = new Vector2(side * 290, JourneyLayout.Y(t));
                if (bannerWorld[used] != w) { bannerWorld[used] = w; bannerTexts[used].text = "WORLD " + (w + 1) + "\n" + JourneyLayout.WorldName(w + 1); }
                if (!banners[used].gameObject.activeSelf) banners[used].gameObject.SetActive(true);
                used++;
            }
            for (; used < BannerPool; used++) if (banners[used].gameObject.activeSelf) banners[used].gameObject.SetActive(false);
        }

        // Floating 3D crystals beside the road, deterministic per slot (never on the road itself).
        private void PlaceDecor(int first, int last)
        {
            int s0 = Mathf.FloorToInt((first - 1) / DecorStep), used = 0;
            for (int s = s0; used < DecorPool && s * DecorStep <= last + 1; s++, used++)
            {
                if (s < 0) { used--; continue; }
                var image = decor[used];
                decorSlot[used] = s;
                float t = s * DecorStep + 0.5f; uint h = Hash((uint)s);
                float side = JourneyLayout.X(t) >= 0 ? -1 : 1, size = 70 + (h % 70);
                image.rectTransform.sizeDelta = new Vector2(size, size);
                decorBase[used] = new Vector2(side * (360 + (h >> 8) % 110), JourneyLayout.Y(t) + ((h >> 16) % 80) - 40);
                image.rectTransform.anchoredPosition = decorBase[used];
                image.color = Palette(h);
                if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
            }
            for (; used < DecorPool; used++) { decorSlot[used] = int.MinValue; if (decor[used].gameObject.activeSelf) decor[used].gameObject.SetActive(false); }
        }

        private static Color Palette(uint h)
        {
            switch (h % 4) { case 0: return new Color32(120, 230, 255, 235); case 1: return new Color32(255, 130, 230, 235); case 2: return new Color32(170, 140, 255, 235); default: return new Color32(255, 214, 120, 235); }
        }

        private static uint Hash(uint x) { x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16; return x; }

        private void Play(int index)
        {
            if (tiles == null || index < 0 || index >= tiles.Count || tiles[index].State == LevelTileState.Locked) return;
            play?.Invoke(index);
        }

        /// <summary>Counts a tap on a locked node; the third quick tap opens it (developer option).</summary>
        public void TapLocked(int index)
        {
            if (DeveloperOpen == null || tiles == null || index < 0 || index >= tiles.Count || tiles[index].State != LevelTileState.Locked) return;
            if (index != lockedIndex) { lockedTaps.Reset(); lockedIndex = index; }
            if (lockedTaps.Tap(Clock())) { lockedIndex = -1; DeveloperOpen(index); }
        }

        /// <summary>Hides the page.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }

        /// <summary>Explicit animation driver (rotating crystals, bobbing marker, glow pulse); allocation-free.</summary>
        public void Tick(float dt)
        {
            if (!IsVisible || !(dt >= 0) || float.IsInfinity(dt)) return;
            clock += dt;
            Refresh();
            livesClock += dt; if (livesClock >= 0.25f) { livesClock = 0; RefreshLives(); }
            if (tiles == null || tiles.Count == 0) return;
            Vector2 at = new Vector2(JourneyLayout.X(current), JourneyLayout.Y(current));
            float bob = Mathf.Sin(clock * 3.2f) * 10f;
            float side = JourneyLayout.X(current) >= 0 ? -1 : 1;   // beside the node, toward the middle of the map
            marker.anchoredPosition = at + new Vector2(side * (JourneyLayout.IsMilestone(current) ? 150 : 138), 34 + bob);
            float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 4f);
            halo.anchoredPosition = at; halo.localScale = Vector3.one * (0.9f + 0.18f * pulse);
            for (int i = 0; i < DecorPool; i++)
            {
                if (decorSlot[i] == int.MinValue) continue;
                uint h = Hash((uint)decorSlot[i]);
                int shape = (int)(h % 5), frame = (int)((clock * (4 + h % 4) + (h >> 4) % 48)) % ThemeBackdrop.GeoFrames;
                var sprite = ThemeBackdrop.GeoSprite(shape * ThemeBackdrop.GeoFrames + frame);
                if (decor[i].sprite != sprite) decor[i].sprite = sprite;
                decor[i].rectTransform.anchoredPosition = decorBase[i] + new Vector2(0, Mathf.Sin(clock * 1.3f + (h % 7)) * 14f);
            }
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
    }
}
