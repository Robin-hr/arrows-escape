using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks display snapshots independently of Unity views.</summary>
    public class LevelSelectModelTests
    {
        private readonly string[] ids = { "a", "b", "c" };
        /// <summary>A new player can select only level one.</summary>
        [Test] public void Fresh() { var t = LevelSelectModel.Build(ids, null); Assert.That(t[0].State, Is.EqualTo(LevelTileState.Unlocked)); Assert.That(t[0].IsResume, Is.True); Assert.That(t[1].State, Is.EqualTo(LevelTileState.Locked)); Assert.That(t[0].BestMistakes, Is.EqualTo(-1)); Assert.That(t[0].BestUndos, Is.EqualTo(-1)); }
        /// <summary>Completion unlocks the next tile and maps the paired best.</summary>
        [Test] public void Completed() { var p = new ProgressTracker(); p.RecordCompletion("a", 2, 5); var t = LevelSelectModel.Build(ids, p); Assert.That(t[0].State, Is.EqualTo(LevelTileState.Completed)); Assert.That(t[0].BestMistakes, Is.EqualTo(2)); Assert.That(t[0].BestUndos, Is.EqualTo(5)); Assert.That(t[1].State, Is.EqualTo(LevelTileState.Unlocked)); Assert.That(t[1].IsResume, Is.True); }
        /// <summary>All completed keeps only the final resume marker.</summary>
        [Test] public void All() { var p = new ProgressTracker(); foreach (string id in ids) p.RecordCompletion(id, 0, 0); var t = LevelSelectModel.Build(ids, p); int resume = 0; for (int i = 0; i < 3; i++) { Assert.That(t[i].State, Is.EqualTo(LevelTileState.Completed)); if (t[i].IsResume) resume++; Assert.That(t[i].Index, Is.EqualTo(i)); Assert.That(t[i].Number, Is.EqualTo(i + 1)); Assert.That(t[i].Id, Is.EqualTo(ids[i])); } Assert.That(resume, Is.EqualTo(1)); Assert.That(t[2].IsResume, Is.True); }
        /// <summary>A completed but out-of-order record still displays completed.</summary>
        [Test] public void OddCompletion() { var p = new ProgressTracker(); p.RecordCompletion("c", 0, 0); p.RecordCompletion("unknown", 1, 1); var t = LevelSelectModel.Build(ids, p); Assert.That(t.Count, Is.EqualTo(3)); Assert.That(t[2].State, Is.EqualTo(LevelTileState.Completed)); Assert.That(t[0].IsResume, Is.True); }
        /// <summary>Missing or empty ids produce no tiles.</summary>
        [Test] public void Empty() { Assert.That(LevelSelectModel.Build(null, null), Is.Empty); Assert.That(LevelSelectModel.Build(Array.Empty<string>(), null), Is.Empty); }
    }
}
