using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies save boundaries without file access.</summary>
    public class ProgressServiceTests
    {
        /// <summary>Loading normalizes data from an injected store.</summary>
        [Test] public void Load() { var data = new ProgressTracker(); data.RecordCompletion("a", 2, 3); var store = new FakeProgressStore { Saved = data.Data }; var service = new ProgressService(store); service.Load(); Assert.That(service.Tracker.IsCompleted("a"), Is.True); Assert.That(store.Loads, Is.EqualTo(1)); Assert.That(store.Saves, Is.Zero); }
        /// <summary>Completion saves once even when the store fails; progress stays in memory.</summary>
        [TestCase(false)][TestCase(true)] public void Record(bool fail)
        { var store = new FakeProgressStore { FailSave = fail }; var service = new ProgressService(store); service.Load(); var result = service.RecordCompletion("a", 1, 2); Assert.That(store.Saves, Is.EqualTo(1)); Assert.That(service.Tracker.Data.lastLevelId, Is.EqualTo("a")); Assert.That(service.LastSaveFailed, Is.EqualTo(fail)); Assert.That(result.IsFirstClear, Is.True); Assert.That(service.Tracker.IsCompleted("a"), Is.True); }
        /// <summary>Remembering a start saves the id without recording completion.</summary>
        [Test] public void Remember() { var store = new FakeProgressStore(); var s = new ProgressService(store); s.RememberLevel("a"); Assert.That(store.Saves, Is.EqualTo(1)); Assert.That(s.Tracker.Data.lastLevelId, Is.EqualTo("a")); Assert.That(s.Tracker.IsCompleted("a"), Is.False); }
        /// <summary>Reset clears and saves.</summary>
        [Test] public void Reset() { var store = new FakeProgressStore(); var s = new ProgressService(store); s.RecordCompletion("a", 0, 0); s.ResetAll(); Assert.That(store.Saves, Is.EqualTo(2)); Assert.That(s.Tracker.Data.levels, Is.Empty); Assert.That(s.Tracker.Data.lastLevelId, Is.Empty); }
        /// <summary>Null stores are programming errors.</summary>
        [Test] public void NullStore() => Assert.Throws<ArgumentNullException>(() => new ProgressService(null));
        /// <summary>A recovered write clears the last-failure flag.</summary>
        [Test] public void Recovery() { var store = new FakeProgressStore { FailSave = true }; var s = new ProgressService(store); s.RememberLevel("a"); Assert.That(s.LastSaveFailed, Is.True); store.FailSave = false; s.RememberLevel("b"); Assert.That(s.LastSaveFailed, Is.False); }
    }
}
