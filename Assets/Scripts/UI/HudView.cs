using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Step 17: paper HUD in the style of the reference — a lavender origami banner on top (coins, LEVEL n with hearts,
    /// arrows left, settings and night buttons) and a mint banner at the bottom (UNDO, HINT, TOOLS tiles). Everything is
    /// drawn from white art tinted by <see cref="SetNight"/> (day pastels or dusk colours). Public members keep the old
    /// API so game flow, tests and the board layout are unchanged.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        // Reference sizes (1080 x 1920 canvas units).
        private const float TopWidth = 1080, TopHeight = 300, BottomWidth = 1080, BottomHeight = 260;
        private RectTransform top, bottom;

        /// <summary>Day palette.</summary>
        public static readonly Color DayInk = new Color32(43, 27, 47, 255), DayTop = new Color32(216, 200, 232, 255), DayTopBack = new Color32(190, 228, 208, 255),
            DayBottom = new Color32(190, 228, 208, 255), DayBottomBack = new Color32(250, 240, 226, 255), DayTile = new Color32(252, 246, 236, 255), DayPill = new Color32(250, 240, 226, 255);
        /// <summary>Night palette.</summary>
        public static readonly Color NightInk = new Color32(242, 234, 247, 255), NightTop = new Color32(84, 70, 104, 255), NightTopBack = new Color32(52, 92, 78, 255),
            NightBottom = new Color32(52, 92, 78, 255), NightBottomBack = new Color32(70, 60, 84, 255), NightTile = new Color32(58, 50, 70, 255), NightPill = new Color32(70, 60, 84, 255);
        /// <summary>True while the dusk palette is shown (dark boards).</summary>
        public bool IsNight { get; private set; }
        /// <summary>Current text and icon colour (dark plum by day, pale lilac by night).</summary>
        public Color InkColor => IsNight ? NightInk : DayInk;
        /// <summary>Kept for older callers: the HUD text colour follows the palette; see <see cref="InkColor"/>.</summary>
        public void SetBoardContrast(bool dark) => SetNight(dark);
        /// <summary>Kept for older callers.</summary>
        public void SetBoardContrast(bool dark, bool themed) => SetNight(dark);
        /// <summary>Legacy static text colour (night ink).</summary>
        public static Color TextColor => NightInk;
        /// <summary>Moves over par but still two stars.</summary>
        public static Color AmberColor => new Color32(222, 136, 20, 255);
        /// <summary>Moves at one star.</summary>
        public static Color RedColor => new Color32(226, 76, 64, 255);
        /// <summary>Live potential-star meter.</summary>
        public StarDisplay Stars { get; private set; }
        /// <summary>Freezes the small loss animation while paused.</summary>
        public bool StarsPaused { get; set; }
        private int lastMoves = -1, lastTotal = -1, lastMistakes = -1, lastStarMoves, lastStarPar;
        /// <summary>Updates the move-budget colour and potential stars.</summary>
        public void SetStarState(int moves, int par, bool reset = false)
        {
            if (Stars == null) return;
            lastStarMoves = moves; lastStarPar = par;
            int count = StarRating.StarsIfClearedNow(moves, par);
            MovesText.color = moves <= par ? InkColor : moves <= StarRating.TwoStarLimit(par) ? AmberColor : RedColor;
            if (reset || Stars.Count != count) Stars.SetCount(count, !reset);
        }
        /// <summary>Explicit animation driver for tests.</summary>
        public void Tick(float dt)
        {
            if (!StarsPaused && Stars != null) Stars.Tick(dt);
            if (noteTime > 0) { noteTime -= dt; float a = Mathf.Clamp01(noteTime / .35f); noteGroup.alpha = a; if (noteTime <= 0) note.gameObject.SetActive(false); }
        }
        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }

        private RectTransform stuck;
        /// <summary>The noninteractive stuck warning text.</summary>
        public UnityEngine.UI.Text StuckText { get; private set; }
        /// <summary>The safe-area warning rectangle for layout checks.</summary>
        public RectTransform StuckBanner => stuck;
        /// <summary>Shows a proven-dead-end warning without blocking undo or restart.</summary>
        public void SetStuckVisible(bool value)
        {
            if (stuck == null || stuck.gameObject.activeSelf == value) return;
            stuck.gameObject.SetActive(value); lastSize = Vector2.zero; UpdateLayout();
        }
        private Vector2 lastSize;
        private string lastName;
        private int lastLevelIndex = -2, lastLevelCount = -1;
        private string lastLevelLabel = "Level";
        /// <summary>Level name (kept updated for flow and tests; the banner shows <see cref="TitleText"/>).</summary>
        public UnityEngine.UI.Text LevelText { get; private set; }
        /// <summary>"Level 2 / 360" (kept updated; the banner shows <see cref="TitleText"/>).</summary>
        public UnityEngine.UI.Text LevelPositionText { get; private set; }
        /// <summary>Step 17: the big banner title, e.g. "LEVEL 28", "JOURNEY 12", "DAILY 25 NOV".</summary>
        public UnityEngine.UI.Text TitleText { get; private set; }
        /// <summary>Moves against par under the hearts.</summary>
        public UnityEngine.UI.Text MovesText { get; private set; }
        /// <summary>Blocked tap count label (hidden; hearts show mistakes).</summary>
        public UnityEngine.UI.Text MistakesText { get; private set; }
        /// <summary>Step 17: coins in the left pill.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>Step 17: arrows still on the board, in the right pill.</summary>
        public UnityEngine.UI.Text ArrowsLeftText { get; private set; }
        /// <summary>Settings (gear): opens the pause menu.</summary>
        public UnityEngine.UI.Button PauseButton { get; private set; }
        /// <summary>Step 17: night board toggle (moon by day, sun by night).</summary>
        public UnityEngine.UI.Button NightButton { get; private set; }
        /// <summary>Undo control (UNDO tile).</summary>
        public UnityEngine.UI.Button UndoButton { get; private set; }
        /// <summary>Hint control (HINT tile).</summary>
        public UnityEngine.UI.Button HintButton { get; private set; }
        /// <summary>Step 17: TOOLS tile; opens the tools tray.</summary>
        public UnityEngine.UI.Button ToolsButton { get; private set; }
        /// <summary>Restart (in the tools tray).</summary>
        public UnityEngine.UI.Button RestartButton { get; private set; }
        /// <summary>Step 17: shows every arrow that can leave right now (in the tools tray, costs one hint).</summary>
        public UnityEngine.UI.Button RevealButton { get; private set; }
        /// <summary>Step 17: the tools tray above the TOOLS tile.</summary>
        public RectTransform ToolsTray { get; private set; }
        /// <summary>Hints left ("+" when none), on the Hint button.</summary>
        public UnityEngine.UI.Text HintCountText { get; private set; }
        /// <summary>Undos left ("+" when none), on the Undo button.</summary>
        public UnityEngine.UI.Text UndoCountText { get; private set; }
        /// <summary>Zoom the board in.</summary>
        public UnityEngine.UI.Button ZoomInButton { get; private set; }
        /// <summary>Zoom the board out.</summary>
        public UnityEngine.UI.Button ZoomOutButton { get; private set; }
        /// <summary>Hearts left in this attempt.</summary>
        public UnityEngine.UI.Image[] Hearts { get; private set; }
        /// <summary>True while the HUD shows the daily-puzzle layout.</summary>
        public bool IsDaily { get; private set; }
        private RectTransform heartRow, zoomColumn, rail;
        private UnityEngine.UI.Image topFront, topBack, bottomFront, bottomBack, trackImage, nightIcon;
        private readonly System.Collections.Generic.List<UnityEngine.UI.Image> tiles = new System.Collections.Generic.List<UnityEngine.UI.Image>(), pills = new System.Collections.Generic.List<UnityEngine.UI.Image>(), inkIcons = new System.Collections.Generic.List<UnityEngine.UI.Image>();
        private readonly System.Collections.Generic.List<UnityEngine.UI.Text> inkTexts = new System.Collections.Generic.List<UnityEngine.UI.Text>();
        private RectTransform note; private CanvasGroup noteGroup; private UnityEngine.UI.Text noteText; private float noteTime;
        /// <summary>The short message under the top banner, for tests.</summary>
        public UnityEngine.UI.Text NoteText => noteText;

        /// <summary>Shows hint and undo counts; zero shows a "+" (tap to get more).</summary>
        public void SetBoosts(int hints, int undos)
        {
            if (HintCountText == null) return;
            string h = hints > 0 ? hints.ToString() : "+", u = undos > 0 ? undos.ToString() : "+";
            if (HintCountText.text != h) HintCountText.text = h;
            if (UndoCountText.text != u) UndoCountText.text = u;
        }
        /// <summary>Step 17: coins in the banner.</summary>
        public void SetCoins(long coins) { if (CoinsText == null) return; string t = coins >= 100000 ? (coins / 1000) + "K" : coins.ToString(); if (CoinsText.text != t) CoinsText.text = t; }
        /// <summary>Step 17: arrows still on the board.</summary>
        public void SetArrowsLeft(int count) { if (ArrowsLeftText == null) return; string t = count.ToString(); if (ArrowsLeftText.text != t) ArrowsLeftText.text = t; }

        /// <summary>Switches between the level layout and the daily layout.</summary>
        public void SetDaily(bool daily)
        {
            if (heartRow == null) return;
            IsDaily = daily;
            lastName = null; lastMoves = lastTotal = lastMistakes = -1; lastLevelIndex = -2;
        }
        /// <summary>Shows the zoom buttons.</summary>
        public void SetZoomVisible(bool visible) { if (zoomColumn != null && zoomColumn.gameObject.activeSelf != visible) zoomColumn.gameObject.SetActive(visible); }

        /// <summary>Daily header: title, date and moves (no par).</summary>
        public void SetDailyInfo(string date, int moves)
        {
            if (LevelText == null) return;
            LevelText.text = "DAILY PUZZLE"; LevelPositionText.text = date; MovesText.text = "Moves " + moves; MovesText.color = InkColor;
            TitleText.text = ("DAILY " + ShortDate(date)).Trim();
        }
        // "25 Nov 2026" -> "25 NOV"
        private static string ShortDate(string date) { if (string.IsNullOrEmpty(date)) return ""; var parts = date.Split(' '); return parts.Length >= 2 ? (parts[0] + " " + parts[1]).ToUpperInvariant() : date.ToUpperInvariant(); }

        /// <summary>Fills the first <paramref name="count"/> hearts.</summary>
        public void SetHearts(int count)
        {
            if (Hearts == null) return;
            for (int i = 0; i < Hearts.Length; i++) { var sprite = BrandArt.SpriteOf(i < count ? "Hud/hud_heart" : "Hud/hud_heart_empty"); if (Hearts[i].sprite != sprite) Hearts[i].sprite = sprite; }
        }

        /// <summary>Step 15: NORMAL / HARD / SUPER HARD label under the top banner (hidden for Normal).</summary>
        public UnityEngine.UI.Text DifficultyText { get; private set; }
        /// <summary>Step 15: the pill behind <see cref="DifficultyText"/>.</summary>
        public UnityEngine.UI.Image DifficultyPill { get; private set; }
        /// <summary>Cleared-arrows progress fill (vertical rail on the right).</summary>
        public UnityEngine.UI.Image ProgressFill { get; private set; }
        /// <summary>Current progress shown by the rail (0..1).</summary>
        public float Progress { get; private set; }
        /// <summary>First-time padlock coach banner.</summary>
        public RectTransform LockTip { get; private set; }
        /// <summary>Shows the difficulty label for the current level.</summary>
        public void SetDifficulty(LevelBadgeKind kind)
        {
            if (DifficultyPill == null) return;
            bool show = kind != LevelBadgeKind.Normal;
            if (DifficultyPill.gameObject.activeSelf != show) DifficultyPill.gameObject.SetActive(show);
            if (!show) return;
            DifficultyText.text = LevelBadge.Label(kind);
            DifficultyPill.color = kind == LevelBadgeKind.SuperHard ? new Color32(214, 38, 110, 255) : new Color32(232, 140, 20, 255);
        }
        /// <summary>Sets the cleared fraction of the board (fills the rail from the bottom).</summary>
        public void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value); if (ProgressFill == null) return;
            var r = ProgressFill.rectTransform; r.anchorMax = new Vector2(1, Progress);
        }
        /// <summary>Shows or hides the padlock coach banner.</summary>
        public void SetLockTipVisible(bool value) { if (LockTip != null && LockTip.gameObject.activeSelf != value) LockTip.gameObject.SetActive(value); }
        /// <summary>Step 17: opens or closes the tools tray.</summary>
        public void SetToolsOpen(bool open) { if (ToolsTray != null && ToolsTray.gameObject.activeSelf != open) ToolsTray.gameObject.SetActive(open); }
        /// <summary>Step 17: whether the tools tray is open.</summary>
        public bool ToolsOpen => ToolsTray != null && ToolsTray.gameObject.activeSelf;
        /// <summary>Step 17: a short message under the top banner for about two seconds.</summary>
        public void ShowNote(string text) { if (note == null) return; noteText.text = text; note.gameObject.SetActive(true); noteGroup.alpha = 1; noteTime = 2.4f; }

        /// <summary>Step 17: day (pastel) or night (dusk) palette; also swaps the moon/sun icon.</summary>
        public void SetNight(bool night)
        {
            if (top == null) return;
            IsNight = night;
            topFront.color = night ? NightTop : DayTop; topBack.color = night ? NightTopBack : DayTopBack;
            bottomFront.color = night ? NightBottom : DayBottom; bottomBack.color = night ? NightBottomBack : DayBottomBack;
            foreach (var t in tiles) t.color = night ? NightTile : DayTile;
            foreach (var p in pills) p.color = night ? NightPill : DayPill;
            trackImage.color = night ? NightPill : DayTop;
            Color ink = InkColor;
            foreach (var i in inkIcons) i.color = ink;
            foreach (var t in inkTexts) t.color = ink;
            nightIcon.sprite = BrandArt.SpriteOf(night ? "Hud/hud_icon_sun" : "Hud/hud_icon_moon");
            if (Stars != null && lastStarPar > 0) SetStarState(lastStarMoves, lastStarPar); else if (MovesText != null) MovesText.color = InkColor;
        }

        private UnityEngine.UI.Image Layer(string name, Transform parent, string art, Vector2 size)
        {
            var img = BrandArt.Image(name, parent, "Hud/" + art, size, Vector2.zero); img.preserveAspect = false; return img;
        }
        private UnityEngine.UI.Text Ink(string name, Transform parent, string value, int size, Vector2 box, Vector2 pos, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = UiFactory.Text(name, parent, value, size, box, pos); t.font = BrandArt.HudFont; t.alignment = anchor; t.color = DayInk;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; inkTexts.Add(t); return t;
        }
        private UnityEngine.UI.Image InkIcon(string name, Transform parent, string art, float size, Vector2 pos)
        {
            var i = BrandArt.Image(name, parent, "Hud/" + art, new Vector2(size, size), pos); i.color = DayInk; inkIcons.Add(i); return i;
        }
        // Paper tile button: tinted tile, ink icon, optional label under it.
        private UnityEngine.UI.Button Tile(string name, Transform parent, Vector2 pos, float size, string icon, float iconSize, string label = null, bool small = false)
        {
            var rect = UiFactory.Rect(name, parent, new Vector2(size, size), pos);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = BrandArt.SpriteOf(small ? "Hud/hud_tile_small" : "Hud/hud_tile"); image.color = DayTile; tiles.Add(image);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; rect.gameObject.AddComponent<ButtonFeel>();
            var colors = button.colors; colors.disabledColor = new Color(1, 1, 1, .55f); colors.pressedColor = new Color(.92f, .92f, .92f, 1); colors.highlightedColor = Color.white; button.colors = colors;
            InkIcon("Icon", rect, icon, iconSize, new Vector2(0, 4));
            // Label text stays the first Text child for older button-text helpers.
            var hidden = UiFactory.Text("Label", rect, name, 10, Vector2.zero, Vector2.zero); hidden.enabled = false;
            if (label != null) Ink(name + "Caption", parent, Spaced(label), 36, new Vector2(size + 60, 50), pos + new Vector2(0, -size * .5f - 22));
            return button;
        }
        private static string Spaced(string s) { var b = new System.Text.StringBuilder(); for (int i = 0; i < s.Length; i++) { if (i > 0) b.Append(' '); b.Append(s[i]); } return b.ToString(); }
        private UnityEngine.UI.Text Badge(Transform button, string art)
        {
            var badge = BrandArt.Image("Count", button, "Hud/" + art, new Vector2(58, 58), Vector2.zero);
            var r = badge.rectTransform; r.anchorMin = r.anchorMax = new Vector2(1, 0); r.anchoredPosition = new Vector2(-8, 10);
            var t = UiFactory.Text("Value", badge.transform, "3", 34, new Vector2(58, 58), new Vector2(0, 2)); t.font = BrandArt.HudFont; t.color = Color.white; t.raycastTarget = false;
            return t;
        }
        private UnityEngine.UI.Image Pill(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var img = BrandArt.Image(name, parent, "Hud/hud_pill_paper", size, pos, new Vector4(60, 60, 60, 60)); img.color = DayPill; pills.Add(img); return img;
        }

        /// <summary>Creates the HUD once under a safe-area rectangle.</summary>
        public void Build(RectTransform safeArea)
        {
            if (top != null) return;
            // --- Top banner ---------------------------------------------------------------------------------------
            top = UiFactory.Rect("TopBar", safeArea, new Vector2(TopWidth, TopHeight), Vector2.zero);
            top.anchorMin = top.anchorMax = top.pivot = new Vector2(0.5f, 1);
            Layer("Shadow", top, "hud_top_shadow", new Vector2(TopWidth, TopHeight));
            topBack = Layer("Back", top, "hud_top_back", new Vector2(TopWidth, TopHeight));
            topFront = Layer("Front", top, "hud_top_front", new Vector2(TopWidth, TopHeight));
            var coinPill = Pill("CoinPill", top, new Vector2(-330, 26), new Vector2(232, 100));
            BrandArt.Image("Coin", coinPill.transform, "Hud/hud_coin", new Vector2(78, 78), new Vector2(-68, 0));
            CoinsText = Ink("Coins", coinPill.transform, "0", 56, new Vector2(140, 90), new Vector2(30, 2));
            var leftPill = Pill("ArrowsPill", top, new Vector2(212, 26), new Vector2(232, 100));
            InkIcon("Coil", leftPill.transform, "hud_icon_coil", 62, new Vector2(-66, 0));
            ArrowsLeftText = Ink("ArrowsLeft", leftPill.transform, "0", 56, new Vector2(140, 90), new Vector2(30, 2));
            TitleText = Ink("Title", top, "LEVEL 1", 70, new Vector2(330, 86), new Vector2(-58, 62));
            TitleText.resizeTextForBestFit = true; TitleText.resizeTextMinSize = 30; TitleText.resizeTextMaxSize = 70; TitleText.horizontalOverflow = HorizontalWrapMode.Wrap; TitleText.verticalOverflow = VerticalWrapMode.Truncate;
            heartRow = UiFactory.Rect("Hearts", top, new Vector2(200, 60), new Vector2(-58, 8));
            Hearts = new UnityEngine.UI.Image[EconomyConfig.DailyHearts];
            for (int i = 0; i < Hearts.Length; i++) Hearts[i] = BrandArt.Image("Heart" + i, heartRow, "Hud/hud_heart", new Vector2(58, 58), new Vector2((i - 1) * 58, 0));
            MovesText = Ink("Moves", top, "", 30, new Vector2(200, 52), new Vector2(-104, -42), TextAnchor.MiddleRight); MovesText.verticalOverflow = VerticalWrapMode.Truncate;
            MovesText.resizeTextForBestFit = true; MovesText.resizeTextMinSize = 18; MovesText.resizeTextMaxSize = 30; MovesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var starRoot = UiFactory.Rect("StarMeter", top, new Vector2(100, 30), new Vector2(42, -42));
            Stars = starRoot.gameObject.AddComponent<StarDisplay>(); Stars.Build(top, 28, 32); starRoot.anchoredPosition = new Vector2(42, -42);
            LevelText = Ink("LevelName", top, "", 30, new Vector2(320, 40), new Vector2(-58, -42)); LevelText.gameObject.SetActive(false);
            LevelPositionText = Ink("LevelPosition", top, "", 30, new Vector2(300, 40), new Vector2(-58, -42)); LevelPositionText.gameObject.SetActive(false);
            MistakesText = Ink("Mistakes", top, "", 30, new Vector2(300, 40), new Vector2(-58, -42)); MistakesText.gameObject.SetActive(false);
            PauseButton = Tile("Pause", top, new Vector2(430, 74), 104, "hud_icon_gear", 64, null, true);
            NightButton = Tile("Night", top, new Vector2(430, -40), 104, "hud_icon_moon", 64, null, true);
            nightIcon = NightButton.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>();
            var pill = UiFactory.Rect("Difficulty", top, new Vector2(250, 50), new Vector2(-58, -118));
            DifficultyPill = pill.gameObject.AddComponent<UnityEngine.UI.Image>(); DifficultyPill.sprite = BrandArt.SpriteOf("Hud/hud_pill_paper", new Vector4(60, 60, 60, 60)); DifficultyPill.type = UnityEngine.UI.Image.Type.Sliced; DifficultyPill.raycastTarget = false;
            DifficultyText = UiFactory.Text("Label", pill, "HARD", 30, new Vector2(250, 50), new Vector2(0, 1)); DifficultyText.color = Color.white; DifficultyText.font = BrandArt.HudFont; DifficultyText.raycastTarget = false;
            pill.gameObject.SetActive(false);

            // --- Bottom banner ------------------------------------------------------------------------------------
            bottom = UiFactory.Rect("BottomBar", safeArea, new Vector2(BottomWidth, BottomHeight), Vector2.zero);
            bottom.anchorMin = bottom.anchorMax = bottom.pivot = new Vector2(0.5f, 0);
            Layer("Shadow", bottom, "hud_bottom_shadow", new Vector2(BottomWidth, BottomHeight));
            bottomBack = Layer("Back", bottom, "hud_bottom_back", new Vector2(BottomWidth, BottomHeight));
            bottomFront = Layer("Front", bottom, "hud_bottom_front", new Vector2(BottomWidth, BottomHeight));
            UndoButton = Tile("Undo", bottom, new Vector2(-314, 34), 150, "hud_icon_back", 92, "UNDO");
            HintButton = Tile("Hint", bottom, new Vector2(0, 34), 150, "hud_icon_hint", 96, "HINT");
            ToolsButton = Tile("Tools", bottom, new Vector2(314, 34), 150, "hud_icon_tools", 96, "TOOLS");
            UndoCountText = Badge(UndoButton.transform, "hud_badge"); HintCountText = Badge(HintButton.transform, "hud_badge");
            // Tools tray: a paper card above the TOOLS tile with RESTART and SHOW FREE ARROWS.
            ToolsTray = UiFactory.Rect("ToolsTray", bottom, new Vector2(420, 220), new Vector2(232, 290));
            var trayCard = ToolsTray.gameObject.AddComponent<UnityEngine.UI.Image>(); trayCard.sprite = BrandArt.SpriteOf("Hud/hud_pill_paper", new Vector4(60, 60, 60, 60)); trayCard.type = UnityEngine.UI.Image.Type.Sliced; trayCard.color = DayPill; pills.Add(trayCard);
            RestartButton = Tile("Restart", ToolsTray, new Vector2(-96, 22), 120, "hud_icon_restart", 74, "RESTART");
            RevealButton = Tile("Reveal", ToolsTray, new Vector2(96, 22), 120, "hud_icon_reveal", 74, "FREE");
            ToolsTray.gameObject.SetActive(false);

            // --- Right rail: progress and zoom -------------------------------------------------------------------
            rail = UiFactory.Rect("Rail", safeArea, new Vector2(120, 760), Vector2.zero);
            rail.anchorMin = rail.anchorMax = rail.pivot = new Vector2(1, .5f);
            var track = UiFactory.Rect("Progress", rail, new Vector2(30, 420), new Vector2(-40, 150));
            trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>(); trackImage.sprite = BrandArt.SpriteOf("Hud/hud_track", new Vector4(30, 30, 30, 30)); trackImage.type = UnityEngine.UI.Image.Type.Sliced; trackImage.raycastTarget = false; trackImage.color = DayTop;
            var fill = UiFactory.Rect("Fill", track, Vector2.zero, Vector2.zero); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(1, 0); fill.pivot = new Vector2(.5f, 0); fill.offsetMin = new Vector2(5, 5); fill.offsetMax = new Vector2(-5, -5);
            ProgressFill = fill.gameObject.AddComponent<UnityEngine.UI.Image>(); ProgressFill.sprite = BrandArt.SpriteOf("Hud/hud_track", new Vector4(30, 30, 30, 30)); ProgressFill.type = UnityEngine.UI.Image.Type.Sliced; ProgressFill.color = new Color32(124, 196, 156, 255); ProgressFill.raycastTarget = false;
            zoomColumn = UiFactory.Rect("Zoom", rail, new Vector2(110, 230), new Vector2(-55, -210));
            ZoomInButton = Tile("ZoomIn", zoomColumn, new Vector2(0, 56), 96, "hud_icon_zoom_in", 58, null, true);
            ZoomOutButton = Tile("ZoomOut", zoomColumn, new Vector2(0, -56), 96, "hud_icon_zoom_out", 58, null, true);
            zoomColumn.gameObject.SetActive(false);

            // --- Notes and banners ---------------------------------------------------------------------------------
            stuck = PaperCard("StuckBanner", safeArea, new Vector2(900, 96));
            stuck.anchorMin = stuck.anchorMax = stuck.pivot = new Vector2(.5f, 1);
            StuckText = Ink("StuckText", stuck, "No way out from here.\nUndo or Restart.", 32, Vector2.zero, Vector2.zero); StuckText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inkTexts.Remove(StuckText); StuckText.color = RedColor; UiFactory.Stretch(StuckText.rectTransform); stuck.gameObject.SetActive(false);
            LockTip = PaperCard("LockTip", safeArea, new Vector2(900, 120));
            LockTip.anchorMin = LockTip.anchorMax = LockTip.pivot = new Vector2(.5f, 0);
            var tipText = Ink("LockTipText", LockTip, "PADLOCK! Clear the arrow with the matching KEY first —\nthen the padlocked arrow is free to go.", 30, Vector2.zero, Vector2.zero);
            tipText.horizontalOverflow = HorizontalWrapMode.Wrap; UiFactory.Stretch(tipText.rectTransform); LockTip.gameObject.SetActive(false);
            note = PaperCard("Note", safeArea, new Vector2(760, 84)); note.anchorMin = note.anchorMax = note.pivot = new Vector2(.5f, 1);
            noteGroup = note.gameObject.AddComponent<CanvasGroup>(); noteGroup.blocksRaycasts = false; noteGroup.interactable = false;
            noteText = Ink("NoteText", note, "", 30, Vector2.zero, Vector2.zero); noteText.horizontalOverflow = HorizontalWrapMode.Wrap; UiFactory.Stretch(noteText.rectTransform); note.gameObject.SetActive(false);
            SetNight(false);
            SetUndoState(false);
            SetHintState(false);
            UpdateLayout();
        }

        private RectTransform PaperCard(string name, Transform parent, Vector2 size)
        {
            var rect = UiFactory.Rect(name, parent, size, Vector2.zero);
            var img = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); img.sprite = BrandArt.SpriteOf("Hud/hud_pill_paper", new Vector4(60, 60, 60, 60)); img.type = UnityEngine.UI.Image.Type.Sliced; img.raycastTarget = false; img.color = DayPill; pills.Add(img);
            return rect;
        }

        /// <summary>Updates only labels whose values changed.</summary>
        public void SetState(string levelName, int moves, int total, int mistakes, bool canUndo)
        {
            if (LevelText == null) return;
            if (lastName != levelName) { lastName = levelName; LevelText.text = DisplayName(levelName); }
            if (lastMoves != moves || lastTotal != total) { lastMoves = moves; lastTotal = total; MovesText.text = $"Moves {moves} / {total}"; }
            if (lastMistakes != mistakes) { lastMistakes = mistakes; MistakesText.text = $"Mistakes {mistakes}"; }
            SetUndoState(canUndo);
        }

        /// <summary>Removes authoring prefixes while preserving ordinary level names.</summary>
        public static string DisplayName(string name)
        { if (string.IsNullOrEmpty(name)) return ""; if (name.StartsWith("Shape ", System.StringComparison.Ordinal)) { int colon = name.IndexOf(':'); if (colon >= 0) return name.Substring(colon + 1).Trim().TrimEnd(':'); } return name.TrimEnd(':'); }

        /// <summary>Sets undo availability.</summary>
        public void SetUndoState(bool canUndo) { if (UndoButton != null) UndoButton.interactable = canUndo; }
        /// <summary>Sets hint availability without changing the board.</summary>
        public void SetHintState(bool canHint) { if (HintButton != null) HintButton.interactable = canHint; if (RevealButton != null) RevealButton.interactable = canHint; }

        /// <summary>Shows one-based progress and the banner title ("LEVEL 28", "JOURNEY 12").</summary>
        public void SetLevelPosition(int index, int count, string label = "Level")
        {
            if (LevelPositionText == null || (lastLevelIndex == index && lastLevelCount == count && lastLevelLabel == label)) return;
            lastLevelIndex = index; lastLevelCount = count; lastLevelLabel = label;
            LevelPositionText.text = index >= 0 && count > 0 ? $"{label} {index + 1} / {count}" : "";
            TitleText.text = index >= 0 && count > 0 ? $"{label.ToUpperInvariant()} {index + 1}" : "PRACTICE";
        }

        private void LateUpdate() => UpdateLayout();
        private void UpdateLayout()
        {
            if (top == null) return;
            Vector2 size = ((RectTransform)top.parent).rect.size;
            if (size == lastSize || size.x <= 0 || size.y <= 0) return;
            lastSize = size;
            // The camera keeps the top 15 % and bottom 16 % free; the banners fill those bands at most.
            float topScale = Mathf.Min(size.x / TopWidth, size.y * .148f / TopHeight);
            if (stuck != null && stuck.gameObject.activeSelf) topScale = Mathf.Min(topScale, size.y * .1f / TopHeight);
            float bottomScale = Mathf.Min(size.x / BottomWidth, size.y * .158f / BottomHeight);
            top.localScale = Vector3.one * topScale; bottom.localScale = Vector3.one * bottomScale;
            top.anchoredPosition = Vector2.zero; bottom.anchoredPosition = Vector2.zero;
            float railScale = Mathf.Min(1f, size.y * .62f / 760f, topScale * 1.1f);
            rail.localScale = Vector3.one * railScale; rail.anchoredPosition = new Vector2(0, -size.y * .02f);
            float bandTop = TopHeight * topScale;
            if (LockTip != null) { LockTip.localScale = Vector3.one * Mathf.Min(UiFit.ComputeScale(900, 120, size.x * .92f, size.y * .07f), 1); LockTip.anchoredPosition = new Vector2(0, BottomHeight * bottomScale + 8); }
            if (stuck != null) { stuck.localScale = Vector3.one * UiFit.ComputeScale(900, 96, size.x * .9f, size.y * .04f); stuck.anchoredPosition = new Vector2(0, -bandTop - 6); }
            if (note != null) { note.localScale = Vector3.one * Mathf.Min(UiFit.ComputeScale(760, 84, size.x * .8f, size.y * .045f), 1); note.anchoredPosition = new Vector2(0, -bandTop - 10); }
        }
    }
}
