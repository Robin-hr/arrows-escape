using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks pure progress and paired best-attempt rules.</summary>
    public class ProgressTrackerTests
    {
        private readonly string[] ids = { "level_001", "level_002", "level_003", "level_004" };
        /// <summary>Only the first level starts unlocked.</summary>
        [Test] public void Empty()
        {
            var t = new ProgressTracker(); Assert.That(t.GetResumeIndex(ids), Is.Zero); Assert.That(t.CompletedCount(ids), Is.Zero);
            Assert.That(t.IsUnlocked(ids, 0), Is.True); Assert.That(t.IsUnlocked(ids, 1), Is.False); Assert.That(t.IsCompleted(ids[0]), Is.False);
        }
        /// <summary>First clear is both first and new best.</summary>
        [Test] public void First()
        { var r = new ProgressTracker().RecordCompletion(ids[0], 3, 2); Assert.That(r.IsFirstClear && r.IsNewBest, Is.True); Assert.That(r.Completions, Is.EqualTo(1)); Assert.That(r.BestMistakes, Is.EqualTo(3)); Assert.That(r.BestUndos, Is.EqualTo(2)); }
        /// <summary>Scores compare lexicographically, preserving one whole attempt.</summary>
        [TestCase(1, 9, true, 1, 9)][TestCase(3, 0, false, 2, 3)][TestCase(2, 2, true, 2, 2)][TestCase(2, 4, false, 2, 3)][TestCase(2, 3, false, 2, 3)]
        public void Best(int mistakes, int undos, bool better, int bestMistakes, int bestUndos)
        {
            var t = new ProgressTracker(); t.RecordCompletion(ids[0], 2, 3); var r = t.RecordCompletion(ids[0], mistakes, undos);
            Assert.That(r.IsFirstClear, Is.False); Assert.That(r.IsNewBest, Is.EqualTo(better)); Assert.That(r.BestMistakes, Is.EqualTo(bestMistakes)); Assert.That(r.BestUndos, Is.EqualTo(bestUndos)); Assert.That(r.Completions, Is.EqualTo(2));
        }
        /// <summary>A perfect attempt cannot be overwritten by worse results.</summary>
        [Test] public void Perfect()
        { var t = new ProgressTracker(); t.RecordCompletion(ids[0], 5, 2); Assert.That(t.RecordCompletion(ids[0], 0, 0).IsNewBest, Is.True); var r = t.RecordCompletion(ids[0], 0, 1); Assert.That(r.IsNewBest, Is.False); Assert.That(r.BestUndos, Is.Zero); Assert.That(r.Completions, Is.EqualTo(3)); }
        /// <summary>Negative attempt inputs clamp to zero.</summary>
        [Test] public void Negative() { var r = new ProgressTracker().RecordCompletion(ids[0], -2, -3); Assert.That(r.BestMistakes, Is.Zero); Assert.That(r.BestUndos, Is.Zero); }
        /// <summary>Invalid ids cannot be recorded.</summary>
        [TestCase(null)][TestCase("")][TestCase(" ")] public void EmptyId(string id) => Assert.Throws<ArgumentException>(() => new ProgressTracker().RecordCompletion(id, 0, 0));
        /// <summary>The immediate predecessor alone controls each unlock; unrelated clears cannot unlock an earlier gap.</summary>
        [Test] public void Unlocks()
        {
            var t = new ProgressTracker(); t.RecordCompletion(ids[2], 0, 0);
            Assert.That(t.IsUnlocked(ids, 2), Is.False); Assert.That(t.IsUnlocked(ids, 3), Is.True);
            Assert.That(t.IsUnlocked(ids, 1), Is.False); t.RecordCompletion(ids[0], 0, 0); Assert.That(t.IsUnlocked(ids, 1), Is.True);
            Assert.That(t.IsUnlocked(ids, -1), Is.False); Assert.That(t.IsUnlocked(ids, 4), Is.False); Assert.That(t.IsUnlocked(null, 0), Is.False);
        }
        /// <summary>Resume selects the first gap and stays on the final level once all are done.</summary>
        [Test] public void Resume()
        {
            var t = new ProgressTracker(); Assert.That(t.GetResumeIndex(null), Is.EqualTo(-1)); Assert.That(t.GetResumeIndex(Array.Empty<string>()), Is.EqualTo(-1));
            t.RecordCompletion(ids[0], 0, 0); t.RecordCompletion(ids[2], 0, 0); Assert.That(t.GetResumeIndex(ids), Is.EqualTo(1));
            t.RecordCompletion(ids[1], 0, 0); Assert.That(t.GetResumeIndex(ids), Is.EqualTo(3)); t.RecordCompletion(ids[3], 0, 0); Assert.That(t.GetResumeIndex(ids), Is.EqualTo(3)); Assert.That(t.CompletedCount(ids), Is.EqualTo(4));
        }
        /// <summary>Stable ids handle both separator styles and optional extensions.</summary>
        [TestCase("Levels/level_002", "level_002")][TestCase("level_002", "level_002")][TestCase("a/b/level_003", "level_003")][TestCase("a\\b\\level_003.json", "level_003")][TestCase("", "")][TestCase(null, "")]
        public void LevelIds(string path, string expected) => Assert.That(ProgressTracker.ToLevelId(path), Is.EqualTo(expected));
        /// <summary>Unknown saved ids are preserved but do not count in this list.</summary>
        [Test] public void UnknownIds() { var t = new ProgressTracker(); t.RecordCompletion("old_level", 0, 0); Assert.That(t.CompletedCount(ids), Is.Zero); Assert.That(t.GetResumeIndex(ids), Is.Zero); Assert.That(t.TryGetRecord("old_level", out _), Is.True); }
        /// <summary>Bad records are dropped and duplicate best pairs merge without independent minima.</summary>
        [Test] public void Sanitize()
        {
            var data = new ProgressData { levels = new List<LevelRecord> {
                null, new LevelRecord { id = "" }, new LevelRecord { id = "bad", completions = -1 },
                new LevelRecord { id = "bad2", bestMistakes = -1 }, new LevelRecord { id = "bad3", bestUndos = -1 },
                new LevelRecord { id = "a", completions = 2, bestMistakes = 3, bestUndos = 0 },
                new LevelRecord { id = "a", completions = 4, bestMistakes = 1, bestUndos = 9 } } };
            var t = new ProgressTracker(data); Assert.That(t.Data.levels.Count, Is.EqualTo(1)); t.TryGetRecord("a", out var r);
            Assert.That(r.completions, Is.EqualTo(6)); Assert.That(r.bestMistakes, Is.EqualTo(1)); Assert.That(r.bestUndos, Is.EqualTo(9));
            Assert.That(new ProgressTracker(new ProgressData { levels = null }).Data.levels, Is.Not.Null);
        }
        /// <summary>Zero is a valid nonnegative completion count; duplicate merging still keeps the better pair.</summary>
        [Test] public void ZeroCompletionDuplicate()
        {
            var data = new ProgressData { levels = new List<LevelRecord> {
                new LevelRecord { id = "a", completions = 2, bestMistakes = 2, bestUndos = 0 },
                new LevelRecord { id = "a", completions = 0, bestMistakes = 0, bestUndos = 9 } } };
            var t = new ProgressTracker(data); t.TryGetRecord("a", out var record);
            Assert.That(record.completions, Is.EqualTo(2)); Assert.That(record.bestMistakes, Is.Zero); Assert.That(record.bestUndos, Is.EqualTo(9));
        }
        /// <summary>Reading records never exposes a mutable internal object.</summary>
        [Test] public void Copy()
        { var t = new ProgressTracker(); t.RecordCompletion("a", 2, 3); t.TryGetRecord("a", out var r); r.bestMistakes = 99; t.TryGetRecord("a", out var again); Assert.That(again.bestMistakes, Is.EqualTo(2)); Assert.That(t.TryGetRecord("missing", out var missing), Is.False); Assert.That(missing, Is.Null); }
        /// <summary>Reset clears records and the last level.</summary>
        [Test] public void Reset() { var t = new ProgressTracker(); t.RecordCompletion("a", 0, 0); t.SetLastLevel("a"); t.Reset(); Assert.That(t.Data.levels, Is.Empty); Assert.That(t.Data.lastLevelId, Is.Empty); Assert.That(t.Data.version, Is.EqualTo(2)); }
    }
}
