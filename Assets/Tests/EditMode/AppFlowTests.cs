using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ArrowEscape.Tests
{
    /// <summary>Drives the real runtime views and screen glue with public button callbacks.</summary>
    public class AppFlowTests
    {
        private FlowTestScene s;
        /// <summary>Removes all isolated app objects.</summary>
        [TearDown] public void Cleanup() => s?.Dispose();
        /// <summary>MainMenu starts with no attempt and blocks board input.</summary>
        [Test] public void Initial()
        {
            s = new FlowTestScene(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu)); Assert.That(s.Flow.MainMenu.Root.gameObject.activeSelf, Is.True);
            Assert.That(s.Board.IsVisible || s.Hud.IsVisible, Is.False); Assert.That(s.Game.Session, Is.Null); Assert.That(s.ProgressStore.Saves, Is.Zero);
            Assert.That(s.Flow.MainMenu.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("PLAY")); Assert.That(s.Flow.MainMenu.PlayLevelText.text, Is.EqualTo("LEVEL 1")); Assert.That(s.Flow.MainMenu.CompletedText.text, Is.EqualTo("Completed 0 / 3"));
            Assert.That(s.Flow.MenusCanvas.name, Is.EqualTo("ArrowEscapeMenus")); Assert.That(s.Flow.MenusCanvas.sortingOrder, Is.EqualTo(100));
        }
        /// <summary>Play starts the first level and exposes only the gameplay display.</summary>
        [Test] public void Play() { s = new FlowTestScene(); s.Flow.MainMenu.PlayButton.onClick.Invoke(); AssertPlaying(0); Assert.That(s.Flow.MainMenu.Root.gameObject.activeSelf, Is.False); }
        /// <summary>Continue chooses the first incomplete resource.</summary>
        [Test] public void Continue()
        {
            var tracker = new ProgressTracker(); tracker.RecordCompletion("level_001", 0, 0); s = new FlowTestScene(new FakeProgressStore { Saved = tracker.Data });
            Assert.That(s.Flow.MainMenu.PlayLevelText.text, Is.EqualTo("LEVEL 2")); Assert.That(s.Flow.MainMenu.ContinueText.text, Is.EqualTo("Level 2")); s.Flow.MainMenu.PlayButton.onClick.Invoke(); AssertPlaying(1);
        }
        /// <summary>Locked tiles cannot start; an unlocked tile starts its exact index.</summary>
        [Test] public void Select()
        {
            s = new FlowTestScene(); s.Flow.MainMenu.LevelsButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.LevelSelect));
            s.Flow.Levels.TileButtons[1].onClick.Invoke(); Assert.That(s.Game.Session, Is.Null); Assert.That(s.Flow.Levels.TileButtons[1].interactable, Is.False);
            s.Flow.Levels.TileButtons[0].onClick.Invoke(); AssertPlaying(0);
        }
        /// <summary>Pause actions resume, restart, or discard without recording a clear.</summary>
        [Test] public void PauseActions()
        {
            s = new FlowTestScene(); s.Flow.MainMenu.PlayButton.onClick.Invoke(); s.Tap(3, 2); s.Hud.Hud.PauseButton.onClick.Invoke();
            Assert.That(s.Flow.PauseScreen.Root.gameObject.activeSelf && s.Game.IsPaused, Is.True); Assert.That(s.Board.IsVisible && s.Hud.IsVisible, Is.True);
            s.Flow.PauseScreen.ResumeButton.onClick.Invoke(); AssertPlaying(0); s.Hud.Hud.PauseButton.onClick.Invoke(); s.Flow.PauseScreen.RestartButton.onClick.Invoke(); AssertPlaying(0); Assert.That(s.Game.MoveCount, Is.Zero);
            s.Tap(3, 2); s.Hud.Hud.PauseButton.onClick.Invoke(); int saves = s.ProgressStore.Saves; s.Flow.PauseScreen.MainMenuButton.onClick.Invoke();
            Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu)); Assert.That(s.Game.Session, Is.Null); Assert.That(s.ProgressStore.Saves, Is.EqualTo(saves)); Assert.That(s.Bootstrap.Progress.Tracker.Data.levels, Is.Empty); Assert.That(s.Board.IsVisible || s.Hud.IsVisible, Is.False);
        }
        /// <summary>Settings preserves either main-menu or pause origin and gameplay pause state.</summary>
        [Test] public void SettingsOrigins()
        {
            s = new FlowTestScene(); s.Flow.MainMenu.SettingsButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Settings)); Assert.That(s.Board.IsVisible, Is.False); s.Flow.SettingsScreen.BackButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
            s.Flow.MainMenu.PlayButton.onClick.Invoke(); s.Hud.Hud.PauseButton.onClick.Invoke(); s.Flow.PauseScreen.SettingsButton.onClick.Invoke(); Assert.That(s.Board.IsVisible && s.Hud.IsVisible && s.Game.IsPaused, Is.True);
            s.Flow.SettingsScreen.BackButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Paused)); Assert.That(s.Game.IsPaused, Is.True);
        }
        /// <summary>Replay, next, and the completion Levels action all preserve screen state.</summary>
        [Test] public void CompletionActions()
        {
            s = new FlowTestScene(); s.Flow.MainMenu.PlayButton.onClick.Invoke(); s.Clear(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.LevelComplete));
            s.Hud.CompletePanel.ReplayButton.onClick.Invoke(); AssertPlaying(0); s.Clear(); s.Hud.CompletePanel.NextButton.onClick.Invoke(); AssertPlaying(1);
            s.Clear(); s.Hud.CompletePanel.LevelsButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.LevelSelect));
            s.Flow.Levels.BackButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
        }
        /// <summary>Back closes confirmation first and otherwise follows screen rules.</summary>
        [Test] public void Back()
        {
            s = new FlowTestScene(); Assert.That(s.Flow.HandleBack(), Is.False); s.Flow.MainMenu.SettingsButton.onClick.Invoke(); s.Flow.SettingsScreen.ResetButton.onClick.Invoke();
            Assert.That(s.Flow.Confirm.IsOpen, Is.True); Assert.That(s.Flow.HandleBack(), Is.True); Assert.That(s.Flow.Confirm.IsOpen, Is.False); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Settings));
            s.Flow.HandleBack(); s.Flow.MainMenu.PlayButton.onClick.Invoke(); s.Flow.HandleBack(); Assert.That(s.Game.IsPaused, Is.True); s.Flow.HandleBack(); AssertPlaying(0);
            s.Clear(); s.Flow.HandleBack(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.LevelSelect)); s.Flow.HandleBack(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
        }
        /// <summary>Application suspension pauses active play only and never auto-resumes.</summary>
        [Test] public void ApplicationPause()
        {
            s = new FlowTestScene(); FlowTestScene.Callback(s.Flow, "OnApplicationPause", true); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
            s.Flow.MainMenu.PlayButton.onClick.Invoke(); FlowTestScene.Callback(s.Flow, "OnApplicationPause", true); Assert.That(s.Game.IsPaused, Is.True); FlowTestScene.Callback(s.Flow, "OnApplicationPause", false); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Paused));
        }
        /// <summary>The app blocker rejects all non-playing screens without replacing the HUD predicate.</summary>
        [Test] public void PointerGate()
        {
            s = new FlowTestScene(); var adapter = s.Game.GetComponent<PointerInputAdapter>(); Vector2 point = new Vector2(-1000, -1000);
            Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False); s.Flow.MainMenu.PlayButton.onClick.Invoke(); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.True);
            s.Hud.Hud.PauseButton.onClick.Invoke(); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False); s.Flow.PauseScreen.SettingsButton.onClick.Invoke(); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False);
            s.Flow.HandleBack(); s.Flow.HandleBack(); s.Clear(); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False); s.Flow.HandleBack(); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False);
        }
        /// <summary>Both UI owners reuse exactly one correctly configured event system.</summary>
        [Test] public void OneEventSystem()
        {
            s = new FlowTestScene(); var system = EventSystemEnsurer.Ensure(); Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(system.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().actionsAsset, Is.Not.Null);
        }
        /// <summary>Reset confirmation refreshes progress without leaving settings.</summary>
        [Test] public void ProgressReset()
        {
            var tracker = new ProgressTracker(); tracker.RecordCompletion("level_001", 2, 0); s = new FlowTestScene(new FakeProgressStore { Saved = tracker.Data });
            s.Flow.MainMenu.SettingsButton.onClick.Invoke(); s.Flow.SettingsScreen.ResetButton.onClick.Invoke(); s.Flow.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(s.Bootstrap.Progress.Tracker.Data.levels, Is.Empty); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Settings)); Assert.That(s.Flow.Levels.TileButtons[1].interactable, Is.False);
            s.Flow.SettingsScreen.BackButton.onClick.Invoke(); Assert.That(s.Flow.MainMenu.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("PLAY")); Assert.That(s.Flow.MainMenu.PlayLevelText.text, Is.EqualTo("LEVEL 1")); Assert.That(s.Flow.MainMenu.CompletedText.text, Is.EqualTo("Completed 0 / 3"));
        }
        /// <summary>Reset from pause replaces the attempt and stays paused through settings Back.</summary>
        [Test] public void ResetWhilePaused()
        { s = new FlowTestScene(); s.Flow.MainMenu.PlayButton.onClick.Invoke(); s.Tap(3, 2); s.Hud.Hud.PauseButton.onClick.Invoke(); s.Flow.PauseScreen.SettingsButton.onClick.Invoke(); s.Flow.SettingsScreen.ResetButton.onClick.Invoke(); s.Flow.Confirm.ConfirmButton.onClick.Invoke(); Assert.That(s.Game.MoveCount, Is.Zero); Assert.That(s.Game.IsPaused, Is.True); s.Flow.SettingsScreen.BackButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Paused)); s.Flow.PauseScreen.ResumeButton.onClick.Invoke(); AssertPlaying(0); }
        /// <summary>HUD/bootstrap Start callbacks are harmless whichever runs before app-flow setup.</summary>
        [TestCase(false)][TestCase(true)] public void StartOrder(bool bootstrapFirst)
        { s = new FlowTestScene(initializeFlow: false); if (bootstrapFirst) FlowTestScene.Callback(s.Bootstrap, "Start"); s.InitializeFlow(); FlowTestScene.Callback(s.Hud, "Start"); if (!bootstrapFirst) FlowTestScene.Callback(s.Bootstrap, "Start"); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu)); Assert.That(s.Game.Session, Is.Null); Assert.That(s.ProgressStore.Loads, Is.EqualTo(1)); }
        /// <summary>Duplicate event-system components and modules are retired without disturbing UI roots.</summary>
        [Test] public void DuplicateEventSystem()
        {
            s = new FlowTestScene(); var duplicate = new GameObject("Duplicate event system", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            try { EventSystemEnsurer.Ensure(); Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1)); }
            finally { Object.DestroyImmediate(duplicate); }
        }
        private void AssertPlaying(int index) { Assert.That(s.Flow.Machine.Current, Is.EqualTo(ScreenState.Playing)); Assert.That(s.Bootstrap.CurrentLevelIndex, Is.EqualTo(index)); Assert.That(s.Board.IsVisible && s.Hud.IsVisible, Is.True); Assert.That(s.Game.IsPaused, Is.False); }
    }
}
