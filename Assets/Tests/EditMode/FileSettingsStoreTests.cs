using System;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Exercises preferences on a unique temporary path, never real player settings.</summary>
    public class FileSettingsStoreTests
    {
        private string folder, path; private FileSettingsStore store;
        /// <summary>Allocates a unique temporary folder name.</summary>
        [SetUp] public void Setup() { folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); path = Path.Combine(folder, "settings.json"); store = new FileSettingsStore(path); }
        /// <summary>Removes only this test's temporary folder.</summary>
        [TearDown] public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        /// <summary>Missing files yield true defaults.</summary>
        [Test] public void Missing() { var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.NoFile)); Assert.That(r.Data.soundEnabled && r.Data.hapticsEnabled, Is.True); }
        /// <summary>Round trips both booleans, creates a folder, and atomically replaces previous data.</summary>
        [Test] public void RoundTrip() { Assert.That(store.TrySave(new SettingsData { soundEnabled = false }), Is.True); Assert.That(Directory.Exists(folder), Is.True); var r = store.Load(); Assert.That(r.Data.soundEnabled, Is.False); Assert.That(r.Data.hapticsEnabled, Is.True); Assert.That(store.TrySave(new SettingsData { hapticsEnabled = false }), Is.True); Assert.That(store.Load().Data.hapticsEnabled, Is.False); Assert.That(File.Exists(path + ".tmp"), Is.False); }
        /// <summary>Corrupt files are backed up with their original bytes.</summary>
        [TestCase("")][TestCase("garbage")][TestCase("{bad}")]
        public void Corrupt(string text) { Directory.CreateDirectory(folder); File.WriteAllText(path, text); byte[] bytes = File.ReadAllBytes(path); var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.Corrupt)); Assert.That(r.Data.soundEnabled && r.Data.hapticsEnabled, Is.True); Assert.That(File.ReadAllBytes(Path.Combine(folder, "settings.corrupt.json")), Is.EqualTo(bytes)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes)); }
        /// <summary>Future settings are copied and remain untouched until saving.</summary>
        [Test] public void Newer() { Directory.CreateDirectory(folder); const string text = "{\"version\":99,\"soundEnabled\":false}"; File.WriteAllText(path, text); var r = store.Load(); Assert.That(r.Status, Is.EqualTo(ProgressLoadStatus.NewerVersion)); Assert.That(r.Data.soundEnabled, Is.True); Assert.That(File.ReadAllText(path), Is.EqualTo(text)); Assert.That(File.ReadAllText(Path.Combine(folder, "settings.newer.json")), Is.EqualTo(text)); Assert.That(store.TrySave(new SettingsData()), Is.True); }
        /// <summary>Impossible paths fail without throwing.</summary>
        [Test] public void Impossible() { Directory.CreateDirectory(folder); string parent = Path.Combine(folder, "file"); File.WriteAllText(parent, "keep"); var bad = new FileSettingsStore(Path.Combine(parent, "settings.json")); Assert.DoesNotThrow(() => Assert.That(bad.TrySave(new SettingsData()), Is.False)); }
        /// <summary>JSON has exactly version and the two preference keys.</summary>
        [Test] public void Format() { store.TrySave(new SettingsData { version = 99, soundEnabled = false }); string json = File.ReadAllText(path); Assert.That(json, Does.Contain("\"version\": 1")); Assert.That(json, Does.Contain("\"soundEnabled\": false")); Assert.That(json, Does.Contain("\"hapticsEnabled\": true")); Assert.That(JsonUtility.FromJson<SettingsData>(json).version, Is.EqualTo(1)); }
        /// <summary>A failed write leaves a good file unchanged.</summary>
        [Test] public void FailedWrite() { store.TrySave(new SettingsData()); string original = File.ReadAllText(path); Directory.CreateDirectory(path + ".tmp"); Assert.That(store.TrySave(new SettingsData { soundEnabled = false }), Is.False); Assert.That(File.ReadAllText(path), Is.EqualTo(original)); }
        /// <summary>Failed backups prevent overwriting the bad original.</summary>
        [Test] public void FailedBackup() { Directory.CreateDirectory(folder); File.WriteAllText(path, "bad"); Directory.CreateDirectory(Path.Combine(folder, "settings.corrupt.json")); Assert.That(store.Load().Status, Is.EqualTo(ProgressLoadStatus.ReadError)); Assert.That(store.TrySave(new SettingsData()), Is.False); Assert.That(File.ReadAllText(path), Is.EqualTo("bad")); }
    }
}
