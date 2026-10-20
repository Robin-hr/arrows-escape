using System;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Uses unique temporary paths only; never the real application save.</summary>
    public class FileProgressStoreTests
    {
        private string folder, path; private FileProgressStore store;
        /// <summary>Creates a unique isolated folder name.</summary>
        [SetUp] public void SetUp() { folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); path = Path.Combine(folder, "progress.json"); store = new FileProgressStore(path); }
        /// <summary>Removes only this test's temporary folder.</summary>
        [TearDown] public void TearDown()
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(folder, true);
        }
        /// <summary>Missing files return safe empty data.</summary>
        [Test] public void Missing() { var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.NoFile)); Assert.That(r.Data.levels, Is.Empty); }
        /// <summary>Every saved field round-trips through the actual JSON serializer.</summary>
        [Test] public void RoundTrip()
        {
            var t = new ProgressTracker(); t.RecordCompletion("level_001", 2, 3); t.RecordCompletion("level_001", 1, 8); t.SetLastLevel("level_002");
            Assert.That(store.TrySave(t.Data), Is.True); var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.Loaded));
            Assert.That(r.Data.version, Is.EqualTo(1)); Assert.That(r.Data.lastLevelId, Is.EqualTo("level_002")); Assert.That(r.Data.levels.Count, Is.EqualTo(1)); var a = r.Data.levels[0]; Assert.That(a.id, Is.EqualTo("level_001")); Assert.That(a.completions, Is.EqualTo(2)); Assert.That(a.bestMistakes, Is.EqualTo(1)); Assert.That(a.bestUndos, Is.EqualTo(8));
        }
        /// <summary>First save creates missing parent folders.</summary>
        [Test] public void CreatesFolder() { Assert.That(Directory.Exists(folder), Is.False); Assert.That(store.TrySave(new ProgressData()), Is.True); Assert.That(File.Exists(path), Is.True); }
        /// <summary>Corrupt files are copied byte-for-byte and the original remains untouched.</summary>
        [TestCase("")][TestCase("garbage")][TestCase("{ broken }")][TestCase("{}")]
        public void Corrupt(string content)
        {
            Directory.CreateDirectory(folder); File.WriteAllText(path, content); var bytes = File.ReadAllBytes(path);
            var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.Corrupt)); Assert.That(r.Data.levels, Is.Empty);
            Assert.That(File.ReadAllBytes(Path.Combine(folder, "progress.corrupt.json")), Is.EqualTo(bytes)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
        }
        /// <summary>Future versions are backed up and never interpreted as version one.</summary>
        [Test] public void Newer()
        {
            Directory.CreateDirectory(folder); const string text = "{\"version\":99,\"levels\":[{\"id\":\"future\",\"completions\":5}]}"; File.WriteAllText(path, text);
            var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.NewerVersion)); Assert.That(r.Data.levels, Is.Empty);
            Assert.That(File.ReadAllText(Path.Combine(folder, "progress.newer.json")), Is.EqualTo(text)); Assert.That(File.ReadAllText(path), Is.EqualTo(text));
            Assert.That(store.TrySave(new ProgressData()), Is.True); Assert.That(File.ReadAllText(Path.Combine(folder, "progress.newer.json")), Is.EqualTo(text));
        }
        /// <summary>Atomic replacement updates an existing file and leaves no temporary file.</summary>
        [Test] public void Replace()
        { Assert.That(store.TrySave(new ProgressData()), Is.True); var data = new ProgressData { lastLevelId = "second" }; Assert.That(store.TrySave(data), Is.True); Assert.That(store.Load().Data.lastLevelId, Is.EqualTo("second")); Assert.That(File.Exists(path + ".tmp"), Is.False); }
        /// <summary>A parent that is a file makes saving fail without throwing.</summary>
        [Test] public void ImpossibleParent()
        { Directory.CreateDirectory(folder); string parent = Path.Combine(folder, "file"); File.WriteAllText(parent, "keep"); var bad = new FileProgressStore(Path.Combine(parent, "progress.json")); Assert.DoesNotThrow(() => Assert.That(bad.TrySave(new ProgressData()), Is.False)); Assert.That(File.ReadAllText(parent), Is.EqualTo("keep")); }
        /// <summary>A failed replacement cannot damage an existing good file or leave a temp behind.</summary>
        [Test] public void FailedWritePreservesGoodFile()
        {
            Assert.That(store.TrySave(new ProgressData { lastLevelId = "good" }), Is.True); byte[] original = File.ReadAllBytes(path);
            // A directory at the required temp-file path deterministically prevents writing on every target OS.
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.TrySave(new ProgressData { lastLevelId = "bad" }), Is.False); Assert.That(File.ReadAllBytes(path), Is.EqualTo(original)); Assert.That(File.Exists(path + ".tmp"), Is.False);
        }
        /// <summary>Version and field names are exactly those of the public save format.</summary>
        [Test] public void ExactFormat()
        {
            var t = new ProgressTracker(); t.RecordCompletion("level_001", 1, 0); t.SetLastLevel("level_001"); t.Data.version = 99;
            Assert.That(store.TrySave(t.Data), Is.True); string json = File.ReadAllText(path);
            foreach (string key in new[] { "version", "lastLevelId", "levels", "id", "completions", "bestMistakes", "bestUndos" }) Assert.That(json, Does.Contain("\"" + key + "\""));
            var parsed = JsonUtility.FromJson<ProgressData>(json); Assert.That(parsed.version, Is.EqualTo(1)); Assert.That(parsed.levels[0].bestMistakes, Is.EqualTo(1));
        }
        /// <summary>Backup names may be overwritten on a subsequent bad load.</summary>
        [Test] public void BackupOverwrite()
        { Directory.CreateDirectory(folder); File.WriteAllText(path, "first bad"); store.Load(); File.WriteAllText(path, "second bad"); store.Load(); Assert.That(File.ReadAllText(Path.Combine(folder, "progress.corrupt.json")), Is.EqualTo("second bad")); }
        /// <summary>Unreadable paths produce a read error and preserve their contents.</summary>
        [Test] public void ReadError()
        { Directory.CreateDirectory(path); var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.ReadError)); Assert.That(r.Data, Is.Not.Null); Assert.That(store.TrySave(new ProgressData()), Is.False); }
        /// <summary>A failed backup prevents later saves from overwriting the original bad file.</summary>
        [Test] public void FailedBackupPreservesOriginal()
        { Directory.CreateDirectory(folder); File.WriteAllText(path, "bad"); Directory.CreateDirectory(Path.Combine(folder, "progress.corrupt.json")); Assert.That(store.Load().Status, Is.EqualTo(ProgressLoadStatus.ReadError)); Assert.That(store.TrySave(new ProgressData()), Is.False); Assert.That(File.ReadAllText(path), Is.EqualTo("bad")); }
    }
}
