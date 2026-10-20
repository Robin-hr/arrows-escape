using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks startup separation, initialization, and the public unlock gate.</summary>
    public class GameBootstrapMenuTests
    {
        private FlowTestScene s;
        /// <summary>Removes isolated objects.</summary>
        [TearDown] public void Cleanup() => s?.Dispose();
        /// <summary>Menu startup loads both services exactly once without starting a board.</summary>
        [Test] public void NoAutoStart() { s = new FlowTestScene(initializeFlow: false); FlowTestScene.Callback(s.Bootstrap, "Start"); s.Bootstrap.EnsureInitialized(); Assert.That(s.Bootstrap.IsInitialized, Is.True); Assert.That(s.Game.Session, Is.Null); Assert.That(s.Bootstrap.LevelIds.Count, Is.EqualTo(3)); Assert.That(s.ProgressStore.Loads, Is.EqualTo(1)); Assert.That(s.SettingsStore.Loads, Is.EqualTo(1)); Assert.That(s.ProgressStore.Saves, Is.Zero); }
        /// <summary>The explicitly enabled manual scene behavior still starts a level.</summary>
        [Test] public void AutoStart() { s = new FlowTestScene(initializeFlow: false); s.SetAutoStart(true); FlowTestScene.Callback(s.Bootstrap, "Start"); Assert.That(s.Bootstrap.CurrentLevelIndex, Is.Zero); Assert.That(s.Game.StartingArrowCount, Is.EqualTo(8)); }
        /// <summary>Locked and out-of-range requests do not replace the current board.</summary>
        [Test] public void Gate() { s = new FlowTestScene(initializeFlow: false); Assert.That(s.Bootstrap.TryStartLevel(0), Is.True); var board = s.Game.Session.Board; Assert.That(s.Bootstrap.TryStartLevel(1), Is.False); Assert.That(s.Bootstrap.TryStartLevel(-1), Is.False); Assert.That(s.Bootstrap.TryStartLevel(3), Is.False); Assert.That(s.Game.Session.Board, Is.SameAs(board)); }
        /// <summary>Continue follows completion state; remembered starts count as progress.</summary>
        [Test] public void Continue() { s = new FlowTestScene(initializeFlow: false); Assert.That(s.Bootstrap.HasAnyProgress, Is.False); Assert.That(s.Bootstrap.ContinueIndex, Is.Zero); s.Bootstrap.TryStartLevel(0); s.Clear(); Assert.That(s.Bootstrap.HasAnyProgress, Is.True); Assert.That(s.Bootstrap.ContinueIndex, Is.EqualTo(1)); }
        /// <summary>A menu reset saves and raises exactly once without starting an attempt.</summary>
        [Test] public void Reset() { s = new FlowTestScene(initializeFlow: false); int resets = 0; s.Bootstrap.ProgressReset += () => resets++; s.Bootstrap.ResetProgress(); Assert.That(resets, Is.EqualTo(1)); Assert.That(s.ProgressStore.Saves, Is.EqualTo(1)); Assert.That(s.Bootstrap.HasAnyProgress, Is.False); Assert.That(s.Game.Session, Is.Null); }
        /// <summary>Injected preference changes survive a service reload.</summary>
        [Test] public void SettingsPersist() { s = new FlowTestScene(initializeFlow: false); s.Bootstrap.Settings.SetSound(false); s.Bootstrap.Settings.Load(); Assert.That(s.Bootstrap.Settings.Model.SoundEnabled, Is.False); }
        /// <summary>Background setup silently tolerates a missing main camera.</summary>
        [Test] public void MissingCamera() { s = new FlowTestScene(initializeFlow: false); var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None); var tags = new string[cameras.Length]; for (int i = 0; i < cameras.Length; i++) { tags[i] = cameras[i].tag; cameras[i].tag = "Untagged"; } try { Assert.DoesNotThrow(() => s.Game.GetComponent<CameraFitter>().ApplyBackground()); } finally { for (int i = 0; i < cameras.Length; i++) cameras[i].tag = tags[i]; } }
    }
}
