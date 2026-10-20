using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks the three-action HUD, availability, and actual screen-space exclusion rectangles.</summary>
    public class HudGuideTests
    {
        private ProgressTestScene s;
        /// <summary>Creates the first sample with real runtime HUD components.</summary>
        [SetUp] public void Setup() { s = new ProgressTestScene(withUi: true); s.Bootstrap.ShowLevel(0); }
        /// <summary>Removes only owned runtime resources.</summary>
        [TearDown] public void Cleanup() => s.Dispose();
        /// <summary>Undo, Hint, and Restart appear in that order; Hint requests one suggestion.</summary>
        [Test] public void Button()
        {
            var hud = s.Hud.Hud; Assert.That(hud.UndoButton.transform.GetSiblingIndex(), Is.LessThan(hud.HintButton.transform.GetSiblingIndex())); Assert.That(hud.HintButton.transform.GetSiblingIndex(), Is.LessThan(hud.RestartButton.transform.GetSiblingIndex()));
            Assert.That(hud.HintButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Hint")); hud.HintButton.onClick.Invoke(); Assert.That(s.Game.HintCount, Is.EqualTo(1)); Assert.That(s.Game.Guide.HasActiveHint, Is.True);
        }
        /// <summary>Level starts and clears refresh availability without disturbing undo or visibility.</summary>
        [Test] public void Availability()
        {
            Assert.That(s.Hud.Hud.HintButton.interactable, Is.True); Assert.That(s.Hud.Hud.UndoButton.interactable, Is.False); s.Clear();
            Assert.That(s.Hud.Hud.HintButton.interactable, Is.False); Assert.That(s.Hud.CompletePanel.IsVisible, Is.True); s.Hud.SetVisible(false); Assert.That(s.Hud.CompletePanel.IsVisible, Is.False);
            s.Bootstrap.RestartLevel(); s.Hud.SetVisible(true); Assert.That(s.Hud.Hud.HintButton.interactable, Is.True); Assert.That(s.Hud.CompletePanel.IsVisible, Is.False);
        }
        /// <summary>Actual world corners fit both the bar and safe area without overlaps in all requested aspects.</summary>
        [TestCase(1080, 1920)][TestCase(1920, 1080)][TestCase(1080, 1080)]
        public void Layout(float width, float height)
        {
            var hud = s.Hud.Hud; Canvas.ForceUpdateCanvases(); var bar = (RectTransform)hud.HintButton.transform.parent; var safe = (RectTransform)bar.parent;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f); safe.sizeDelta = new Vector2(width, height);
            FlowTestScene.Callback(hud, "LateUpdate"); Canvas.ForceUpdateCanvases();
            var buttons = new[] { hud.UndoButton, hud.HintButton, hud.RestartButton }; float previousRight = float.NegativeInfinity;
            foreach (var button in buttons)
            {
                var rect = (RectTransform)button.transform; Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(200)); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                float left = float.PositiveInfinity, right = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = bar.InverseTransformPoint(corner); Vector3 safePoint = safe.InverseTransformPoint(corner);
                    Assert.That(local.x, Is.InRange(bar.rect.xMin - 0.001f, bar.rect.xMax + 0.001f)); Assert.That(local.y, Is.InRange(bar.rect.yMin - 0.001f, bar.rect.yMax + 0.001f));
                    Assert.That(safePoint.x, Is.InRange(safe.rect.xMin - 0.001f, safe.rect.xMax + 0.001f)); Assert.That(safePoint.y, Is.InRange(safe.rect.yMin - 0.001f, safe.rect.yMax + 0.001f));
                    left = Mathf.Min(left, local.x); right = Mathf.Max(right, local.x);
                }
                Assert.That(left, Is.GreaterThan(previousRight)); previousRight = right;
            }
        }
        /// <summary>Hint and the pre-existing Pause rectangle are both blocked from board presses.</summary>
        [Test] public void PointerRects()
        {
            Canvas.ForceUpdateCanvases(); var adapter = s.Game.GetComponent<PointerInputAdapter>();
            foreach (var button in new[] { s.Hud.Hud.HintButton, s.Hud.Hud.PauseButton })
            { var rect = (RectTransform)button.transform; Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); Assert.That(s.Hud.IsBlocked(point), Is.True); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False); }
        }
        /// <summary>Screen-flow pause freezes guides, disables Hint, then resumes both without changing gameplay.</summary>
        [Test] public void FlowPause()
        {
            using (var scene = new FlowTestScene())
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke(); scene.Hud.Hud.HintButton.onClick.Invoke(); scene.Tap(0, 0); scene.Hud.Hud.PauseButton.onClick.Invoke();
                Assert.That(scene.Game.Guide.IsPaused, Is.True); Assert.That(scene.Hud.Hud.HintButton.interactable, Is.False); scene.Game.Guide.AdvanceFrame(10); Assert.That(scene.Game.Guide.HasActiveLine && scene.Game.Guide.HasActiveHint, Is.True);
                scene.Flow.PauseScreen.ResumeButton.onClick.Invoke(); Assert.That(scene.Hud.Hud.HintButton.interactable, Is.True); scene.Game.Guide.AdvanceFrame(10); Assert.That(scene.Game.Guide.HasActiveLine || scene.Game.Guide.HasActiveHint, Is.False);
            }
        }
    }
}
