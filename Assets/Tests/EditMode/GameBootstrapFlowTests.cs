using System.Reflection;
using System.Text.RegularExpressions;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Exercises resume, unlocking, completion, and exact save/event boundaries.</summary>
    public class GameBootstrapFlowTests
    {
        private ProgressTestScene scene;
        /// <summary>Removes only this test's objects.</summary>
        [TearDown] public void TearDown() => scene?.Dispose();
        /// <summary>Fresh progress starts at the first resource.</summary>
        [Test] public void FirstLaunch() { scene = new ProgressTestScene(); scene.Bootstrap.StartInitialLevel(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.Zero); Assert.That(scene.Bootstrap.CurrentLevelId, Is.EqualTo("level_001")); Assert.That(scene.Bootstrap.LevelCount, Is.EqualTo(3)); Assert.That(scene.Store.Saves, Is.EqualTo(1)); }
        /// <summary>Startup resumes the first incomplete level, or the final one if all are completed.</summary>
        [TestCase(1, 1)][TestCase(2, 2)][TestCase(3, 2)] public void Resume(int completed, int expected)
        {
            var tracker = new ProgressTracker(); for (int i = 1; i <= completed; i++) tracker.RecordCompletion("level_00" + i, 0, 0);
            scene = new ProgressTestScene(new FakeProgressStore { Saved = tracker.Data }); scene.Bootstrap.StartInitialLevel(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(expected));
        }
        /// <summary>The Inspector override deliberately ignores saved resume state.</summary>
        [Test] public void InspectorIndex()
        {
            scene = new ProgressTestScene(); typeof(GameBootstrap).GetField("useSavedProgress", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scene.Bootstrap, false);
            typeof(GameBootstrap).GetField("startLevelIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scene.Bootstrap, 1);
            scene.Bootstrap.StartInitialLevel(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(1));
        }
        /// <summary>Final counters are recorded once and replay can improve the paired best.</summary>
        [Test] public void CompletionAndReplay()
        {
            scene = new ProgressTestScene(); var b = scene.Bootstrap; b.ShowLevel(0); int events = 0; CompletionResult last = null; b.LevelCompleted += r => { events++; last = r; };
            scene.Tap(0, 0); scene.ExitOne(); scene.Game.Undo(); int saves = scene.Store.Saves;
            scene.Clear(); Assert.That(events, Is.EqualTo(1)); Assert.That(last.BestMistakes, Is.EqualTo(1)); Assert.That(last.BestUndos, Is.EqualTo(1)); Assert.That(last.Completions, Is.EqualTo(1)); Assert.That(scene.Store.Saves, Is.EqualTo(saves + 1));
            Assert.That(b.CanGoNext, Is.True); b.RestartLevel(); scene.Clear(); Assert.That(events, Is.EqualTo(2)); Assert.That(last.IsNewBest, Is.True); Assert.That(last.IsFirstClear, Is.False); Assert.That(last.Completions, Is.EqualTo(2)); Assert.That(last.BestMistakes, Is.Zero); Assert.That(last.BestUndos, Is.Zero);
        }
        /// <summary>Locked next is a no-op; clearing unlocks exactly the next index.</summary>
        [Test] public void NextUnlock()
        { scene = new ProgressTestScene(); var b = scene.Bootstrap; b.ShowLevel(0); Board original = scene.Game.Session.Board; b.NextLevel(); Assert.That(scene.Game.Session.Board, Is.SameAs(original)); scene.Clear(); b.NextLevel(); Assert.That(b.CurrentLevelIndex, Is.EqualTo(1)); Assert.That(scene.Game.IsCleared, Is.False); b.NextLevel(); Assert.That(b.CurrentLevelIndex, Is.EqualTo(1)); }
        /// <summary>Gameplay methods stop at list bounds; debug methods wrap and ignore locks.</summary>
        [Test] public void BoundariesAndDebug()
        {
            scene = new ProgressTestScene(); var b = scene.Bootstrap; b.ShowLevel(0); Board first = scene.Game.Session.Board; b.PreviousLevel(); Assert.That(scene.Game.Session.Board, Is.SameAs(first));
            b.DebugJump(-1); Assert.That(b.CurrentLevelIndex, Is.EqualTo(2)); Assert.That(b.IsLastLevel, Is.True); Board last = scene.Game.Session.Board; b.NextLevel(); Assert.That(scene.Game.Session.Board, Is.SameAs(last));
            b.DebugJump(1); Assert.That(b.CurrentLevelIndex, Is.Zero); b.DebugJump(2); b.DebugJump(-1); Assert.That(b.CurrentLevelIndex, Is.EqualTo(1));
        }
        /// <summary>Switching and replay never double-subscribe completion handling.</summary>
        [Test] public void ExactlyOnce()
        {
            scene = new ProgressTestScene(); var b = scene.Bootstrap; b.ShowLevel(0); b.ShowLevel(1); b.ShowLevel(0); scene.Clear();
            // Simulate an accidental repeated notification of the same logical clear.
            var field = typeof(GameController).GetField("BoardCleared", BindingFlags.Instance | BindingFlags.NonPublic);
            ((System.Action)field.GetValue(scene.Game)).Invoke();
            b.RestartLevel(); scene.Clear(); b.Progress.Tracker.TryGetRecord("level_001", out var r); Assert.That(r.completions, Is.EqualTo(2));
        }
        /// <summary>Both bootstrap and controller restarts remember exactly one start.</summary>
        [Test] public void RememberOnce()
        {
            scene = new ProgressTestScene(); var b = scene.Bootstrap; b.ShowLevel(0); Assert.That(scene.Store.Saves, Is.EqualTo(1)); scene.Game.RestartLevel(); Assert.That(scene.Store.Saves, Is.EqualTo(2)); b.RestartLevel(); Assert.That(scene.Store.Saves, Is.EqualTo(3)); Assert.That(scene.Store.Saved.lastLevelId, Is.EqualTo("level_001"));
        }
        /// <summary>Reset clears progress and restarts the first level.</summary>
        [Test] public void ResetProgress()
        { scene = new ProgressTestScene(); scene.Bootstrap.ShowLevel(0); scene.Clear(); scene.Bootstrap.NextLevel(); scene.Bootstrap.ResetProgress(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.Zero); Assert.That(scene.Bootstrap.Progress.Tracker.Data.levels, Is.Empty); Assert.That(scene.Store.Saved.lastLevelId, Is.EqualTo("level_001")); }
        /// <summary>Failed saves warn once while completion still unlocks play in memory.</summary>
        [Test] public void FailedSaveWarnsOnce()
        {
            scene = new ProgressTestScene(new FakeProgressStore { FailSave = true }); LogAssert.Expect(LogType.Warning, "ArrowEscape progress could not be saved. Play continues with in-memory progress.");
            scene.Bootstrap.ShowLevel(0); scene.Clear(); scene.Bootstrap.NextLevel(); scene.Bootstrap.RestartLevel(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(1)); Assert.That(scene.Bootstrap.Progress.LastSaveFailed, Is.True);
        }
        /// <summary>A load diagnostic warns once and still starts an empty playable attempt.</summary>
        [TestCase(ProgressLoadStatus.Corrupt)][TestCase(ProgressLoadStatus.NewerVersion)][TestCase(ProgressLoadStatus.ReadError)]
        public void FailedLoad(ProgressLoadStatus status)
        {
            LogAssert.Expect(LogType.Warning, new Regex("ArrowEscape progress: test issue.*progress.corrupt.json.*progress.newer.json"));
            scene = new ProgressTestScene(new FakeProgressStore { Result = new ProgressLoadResult(null, status, "test issue") }); scene.Bootstrap.StartInitialLevel(); Assert.That(scene.Game.StartingArrowCount, Is.EqualTo(8));
        }
    }
}
