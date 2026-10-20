using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks explicit HUD visibility and pause requests.</summary>
    public class HudControllerVisibilityTests
    {
        private ProgressTestScene s;
        /// <summary>Builds an isolated HUD and first sample.</summary>
        [SetUp] public void Setup() { s = new ProgressTestScene(withUi: true); s.Bootstrap.ShowLevel(0); }
        /// <summary>Removes isolated objects.</summary>
        [TearDown] public void Cleanup() => s.Dispose();
        /// <summary>Only a cleared board restores the completion panel when showing the HUD.</summary>
        [Test] public void Visibility() { s.Hud.SetVisible(false); Assert.That(s.Hud.IsVisible, Is.False); Assert.That(s.Hud.CompletePanel.IsVisible, Is.False); s.Hud.SetVisible(true); Assert.That(s.Hud.CompletePanel.IsVisible, Is.False); s.Clear(); s.Hud.SetVisible(false); Assert.That(s.Hud.CompletePanel.IsVisible, Is.False); s.Hud.SetVisible(true); Assert.That(s.Hud.IsVisible && s.Hud.CompletePanel.IsVisible, Is.True); }
        /// <summary>The pause button raises one request.</summary>
        [Test] public void PauseRequest() { int count = 0; s.Hud.PauseRequested += () => count++; s.Hud.Hud.PauseButton.onClick.Invoke(); Assert.That(count, Is.EqualTo(1)); }
    }
}
