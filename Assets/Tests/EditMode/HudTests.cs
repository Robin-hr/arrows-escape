using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Tests runtime UI state and buttons without a hardware EventSystem.</summary>
    public class HudTests
    {
        private GameObject host, uiHost, cameraHost;
        private GameController game;
        private GameBootstrap bootstrap;
        private HudController hud;
        private BoardView view;
        /// <summary>Creates runtime gameplay and an independently bound UI.</summary>
        [SetUp] public void SetUp()
        {
            cameraHost = new GameObject("Test camera", typeof(Camera)); cameraHost.tag = "MainCamera";
            host = new GameObject("Gameplay"); bootstrap = host.AddComponent<GameBootstrap>(); bootstrap.SetLevelCatalog(OriginalTestCatalog.Paths, OriginalTestCatalog.LegacyPaths); game = host.GetComponent<GameController>(); view = host.GetComponent<BoardView>();
            uiHost = new GameObject("UI"); hud = uiHost.AddComponent<HudController>();
        }
        /// <summary>Destroys only the objects this test created.</summary>
        [TearDown] public void TearDown() { Object.DestroyImmediate(uiHost); Object.DestroyImmediate(host); Object.DestroyImmediate(cameraHost); SpriteFactory.ClearCache(); }
        private Board Chain() { var b = new Board(3, 1); for (int i = 0; i < 3; i++) b.TryAddArrow(i, i, 0, Direction.Right, out _); return b; }
        private void Tap(int x) { Vector2 p = view.CellToWorld(x, 0); var taps = host.GetComponent<TapController>(); taps.PointerDown(1, p); taps.PointerUp(1, p); }
        private void ClearChain() { Tap(2); Tap(1); LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: " + game.MistakeCount); Tap(0); }
        /// <summary>Pull and events both produce correct labels regardless of start order.</summary>
        [TestCase(false)][TestCase(true)] public void StartOrder(bool levelFirst)
        {
            if (levelFirst) game.StartLevel(Chain(), "Test Level");
            hud.Bind(game, bootstrap);
            if (!levelFirst) game.StartLevel(Chain(), "Test Level");
            Assert.That(hud.Hud.LevelText.text, Is.EqualTo("Test Level")); Assert.That(hud.Hud.MovesText.text, Is.EqualTo("Moves 0 / 3")); Assert.That(hud.Hud.MistakesText.text, Is.EqualTo("Mistakes 0")); Assert.That(hud.CompletePanel.IsVisible, Is.False);
        }
        /// <summary>Undo and restart buttons invoke gameplay, update counters, and dim appropriately.</summary>
        [Test] public void Buttons()
        {
            game.StartLevel(Chain(), "Chain"); hud.Bind(game, bootstrap); Assert.That(hud.Hud.UndoButton.interactable, Is.False);
            Tap(0); Tap(2); Assert.That(hud.Hud.UndoButton.interactable, Is.True); Assert.That(hud.Hud.MovesText.text, Is.EqualTo("Moves 2 / 3"));
            hud.Hud.UndoButton.onClick.Invoke(); Assert.That(game.MoveCount, Is.Zero); Assert.That(game.MistakeCount, Is.EqualTo(1)); Assert.That(hud.Hud.UndoButton.interactable, Is.False);
            hud.Hud.RestartButton.onClick.Invoke(); Assert.That(game.MistakeCount, Is.Zero); Assert.That(game.UndoCount, Is.Zero); Assert.That(game.Session.Board.Arrows.Count, Is.EqualTo(3));
        }
        /// <summary>Final panel counters include mistakes and total undos, then hide on restart.</summary>
        [Test] public void Completion()
        {
            game.StartLevel(Chain(), "Chain"); hud.Bind(game, bootstrap); Tap(0); Tap(2); game.Undo(); ClearChain();
            Assert.That(hud.CompletePanel.IsVisible, Is.True); Assert.That(hud.CompletePanel.MistakesText.text, Is.EqualTo("Mistakes 1")); Assert.That(hud.CompletePanel.UndosText.text, Is.EqualTo("Undos 1")); Assert.That(hud.CompletePanel.FlawlessText.gameObject.activeSelf, Is.False);
            game.RestartLevel(); Assert.That(hud.CompletePanel.IsVisible, Is.False);
        }
        /// <summary>Flawless requires both counters to be zero.</summary>
        [TestCase(0, 0, true)][TestCase(1, 0, false)][TestCase(0, 1, false)][TestCase(1, 1, false)]
        public void Flawless(int mistakes, int undos, bool expected)
        { hud.Bind(game, bootstrap); hud.CompletePanel.Show(mistakes, undos); Assert.That(hud.CompletePanel.FlawlessText.gameObject.activeSelf, Is.EqualTo(expected)); }
        /// <summary>Button rects block points; board space is open until the modal appears.</summary>
        [Test] public void PointerBlocking()
        {
            game.StartLevel(Chain()); hud.Bind(game, bootstrap); Canvas.ForceUpdateCanvases();
            RectTransform rect = (RectTransform)hud.Hud.UndoButton.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Assert.That(hud.IsBlocked(point), Is.True); Assert.That(host.GetComponent<PointerInputAdapter>().ShouldAcceptPointerDown(point), Is.False);
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); Assert.That(hud.IsBlocked(center), Is.False);
            hud.CompletePanel.Show(0, 0); Assert.That(hud.IsBlocked(new Vector2(-999, -999)), Is.True); Assert.That(hud.IsBlocked(center), Is.True);
        }
        /// <summary>Replay and Next Level start fresh named resource boards.</summary>
        [TestCase(false)][TestCase(true)] public void ReplayNext(bool next)
        {
            bootstrap.ShowLevel(0); hud.Bind(game, bootstrap);
            Board board = game.Session.Board;
            while (!game.IsCleared)
            {
                Arrow free = null; foreach (Arrow a in board.Arrows) if (board.CanExit(a)) { free = a; break; }
                if (board.Arrows.Count == 1) LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0");
                Vector2 pos = view.CellToWorld(free.X, free.Y); var taps = host.GetComponent<TapController>(); taps.PointerDown(1, pos); taps.PointerUp(1, pos);
            }
            Assert.That(hud.CompletePanel.IsVisible, Is.True);
            if (next) hud.CompletePanel.NextButton.onClick.Invoke(); else hud.CompletePanel.ReplayButton.onClick.Invoke();
            Assert.That(game.LevelName, Is.EqualTo(next ? "Level 2" : "Level 1")); Assert.That(game.MoveCount, Is.Zero); Assert.That(game.MistakeCount, Is.Zero); Assert.That(game.UndoCount, Is.Zero); Assert.That(game.IsCleared, Is.False); Assert.That(hud.CompletePanel.IsVisible, Is.False); Assert.That(game.StartingArrowCount, Is.EqualTo(next ? 13 : 8));
        }
        /// <summary>Binding after completion still shows its final summary.</summary>
        [Test] public void SubscribeAfterCleared() { game.StartLevel(Chain()); ClearChain(); hud.Bind(game, bootstrap); Assert.That(hud.CompletePanel.IsVisible, Is.True); }
        /// <summary>Canvas uses the requested overlay scaler and raycaster.</summary>
        [Test] public void CanvasConfiguration()
        {
            hud.Bind(game, bootstrap); Canvas canvas = uiHost.GetComponentInChildren<Canvas>(); var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay)); Assert.That(scaler.uiScaleMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)); Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 1920))); Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f)); Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
        }
        /// <summary>Aspect changes keep text and buttons inside the reserved camera margins.</summary>
        [TestCase(1080, 1920)][TestCase(1920, 1080)][TestCase(1080, 1080)] public void MarginLayout(float width, float height)
        {
            hud.Bind(game, bootstrap); Canvas.ForceUpdateCanvases();
            RectTransform safe = (RectTransform)hud.Hud.UndoButton.transform.parent.parent;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f); safe.sizeDelta = new Vector2(width, height);
            typeof(HudView).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(hud.Hud, null);
            AssertInsideBand(hud.Hud.LevelText.rectTransform, safe, height * 0.35f, height * 0.5f);
            AssertInsideBand(hud.Hud.MovesText.rectTransform, safe, height * 0.35f, height * 0.5f);
            AssertInsideBand((RectTransform)hud.Hud.UndoButton.transform, safe, -height * 0.5f, -height * 0.34f);
        }
        private static void AssertInsideBand(RectTransform rect, RectTransform safe, float min, float max)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners) { Vector3 local = safe.InverseTransformPoint(corner); Assert.That(local.y, Is.InRange(min - 0.01f, max + 0.01f)); Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(safe.rect.width * 0.5f)); }
        }
    }
}
