using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies best-attempt text, final-level flow, and subscription ordering.</summary>
    public class HudProgressTests
    {
        private ProgressTestScene scene;
        /// <summary>Removes isolated runtime objects.</summary>
        [TearDown] public void TearDown() => scene?.Dispose();
        /// <summary>The resource index is displayed one-based next to the JSON name.</summary>
        [Test] public void LevelPosition() { scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(1); Assert.That(scene.Hud.Hud.LevelPositionText.text, Is.EqualTo("Level 2 / 3")); Assert.That(scene.Hud.Hud.LevelText.text, Is.EqualTo("Level 2")); }
        /// <summary>First clear, strict improvement, and worse attempts show the appropriate notice and paired best.</summary>
        [Test] public void BestNotices()
        {
            scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(0); scene.Tap(0, 0); scene.ExitOne(); scene.Game.Undo(); scene.Clear();
            var panel = scene.Hud.CompletePanel; Assert.That(panel.BestNoticeText.text, Is.EqualTo("First clear!")); Assert.That(panel.BestText.text, Is.EqualTo("Best: 1 mistakes, 1 undos"));
            panel.ReplayButton.onClick.Invoke(); scene.Clear(); Assert.That(panel.BestNoticeText.text, Is.EqualTo("New best!")); Assert.That(panel.BestText.text, Is.EqualTo("Best: 0 mistakes")); Assert.That(panel.FlawlessText.gameObject.activeSelf, Is.True);
            panel.ReplayButton.onClick.Invoke(); scene.Tap(0, 0); scene.Clear(); Assert.That(panel.BestNoticeText.gameObject.activeSelf, Is.False); Assert.That(panel.BestText.text, Is.EqualTo("Best: 0 mistakes"));
        }
        /// <summary>Completing only the last level does not claim the whole list is complete.</summary>
        [Test] public void LastWithGaps()
        { scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(2); scene.Clear(); Assert.That(scene.Hud.CompletePanel.NextButton.gameObject.activeSelf, Is.False); Assert.That(scene.Hud.CompletePanel.AllCompleteText.gameObject.activeSelf, Is.False); }
        /// <summary>Completing the whole list hides Next and displays all-complete.</summary>
        [Test] public void AllComplete()
        {
            scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(0);
            for (int i = 0; i < 3; i++) { scene.Clear(); if (i < 2) scene.Hud.CompletePanel.NextButton.onClick.Invoke(); }
            Assert.That(scene.Hud.CompletePanel.NextButton.gameObject.activeSelf, Is.False); Assert.That(scene.Hud.CompletePanel.AllCompleteText.gameObject.activeSelf, Is.True);
            Board last = scene.Game.Session.Board; scene.Hud.CompletePanel.NextButton.onClick.Invoke(); Assert.That(scene.Game.Session.Board, Is.SameAs(last)); Assert.That(scene.Hud.CompletePanel.IsVisible, Is.True);
        }
        /// <summary>Next loads exactly one newly unlocked level and hides the panel.</summary>
        [Test] public void NextUnlocked()
        { scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(0); scene.Clear(); scene.Hud.CompletePanel.NextButton.onClick.Invoke(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(1)); Assert.That(scene.Hud.CompletePanel.IsVisible, Is.False); scene.Hud.CompletePanel.NextButton.onClick.Invoke(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(1)); }
        /// <summary>Either completion subscriber order preserves the persisted-best display.</summary>
        [TestCase(false)][TestCase(true)] public void SubscriptionOrder(bool bindBeforeInitialize)
        { scene = new ProgressTestScene(withUi: true, bindBeforeInitialize: bindBeforeInitialize); scene.Bootstrap.ShowLevel(0); scene.Clear(); Assert.That(scene.Hud.CompletePanel.BestNoticeText.text, Is.EqualTo("First clear!")); Assert.That(scene.Hud.CompletePanel.BestText.gameObject.activeSelf, Is.True); }
        /// <summary>A late UI binding pulls the existing completion without recording another clear.</summary>
        [Test] public void LateBinding()
        { scene = new ProgressTestScene(withUi: true); scene.Bootstrap.ShowLevel(0); scene.Clear(); int saves = scene.Store.Saves; scene.Hud.Bind(scene.Game, scene.Bootstrap); Assert.That(scene.Hud.CompletePanel.BestNoticeText.text, Is.EqualTo("First clear!")); Assert.That(scene.Store.Saves, Is.EqualTo(saves)); }
    }
}
