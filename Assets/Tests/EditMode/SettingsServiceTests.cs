using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies change-only writes and in-memory preference recovery.</summary>
    public class SettingsServiceTests
    {
        /// <summary>Loads saved preferences.</summary>
        [Test] public void Load() { var store = new FakeSettingsStore { Saved = new SettingsData { soundEnabled = false } }; var s = new SettingsService(store); s.Load(); Assert.That(s.Model.SoundEnabled, Is.False); Assert.That(s.Model.HapticsEnabled, Is.True); Assert.That(store.Loads, Is.EqualTo(1)); }
        /// <summary>Only changed values write; failed writes retain changed preferences.</summary>
        [TestCase(false)][TestCase(true)] public void Save(bool fail) { var store = new FakeSettingsStore { FailSave = fail }; var s = new SettingsService(store); s.Load(); s.SetSound(true); Assert.That(store.Saves, Is.Zero); s.SetSound(false); s.SetSound(false); Assert.That(store.Saves, Is.EqualTo(1)); s.SetHaptics(false); s.SetHaptics(false); Assert.That(store.Saves, Is.EqualTo(2)); Assert.That(s.Model.SoundEnabled || s.Model.HapticsEnabled, Is.False); Assert.That(s.LastSaveFailed, Is.EqualTo(fail)); }
        /// <summary>Null storage is a programming error.</summary>
        [Test] public void NullStore() => Assert.Throws<ArgumentNullException>(() => new SettingsService(null));
    }
}
