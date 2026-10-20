using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks true defaults and change-only notifications.</summary>
    public class SettingsModelTests
    {
        /// <summary>New and null data both use true defaults.</summary>
        [Test] public void Defaults() { var m = new SettingsModel(null); Assert.That(new SettingsData().soundEnabled, Is.True); Assert.That(m.SoundEnabled && m.HapticsEnabled, Is.True); Assert.That(m.Data.version, Is.EqualTo(1)); }
        /// <summary>Only actual changes raise notifications and update the payload.</summary>
        [Test] public void Changed() { var m = new SettingsModel(); int n = 0; m.Changed += () => n++; m.SoundEnabled = true; m.HapticsEnabled = true; Assert.That(n, Is.Zero); m.SoundEnabled = false; m.SoundEnabled = false; m.HapticsEnabled = false; Assert.That(n, Is.EqualTo(2)); Assert.That(m.Data.soundEnabled || m.Data.hapticsEnabled, Is.False); }
        /// <summary>Loaded false values remain false.</summary>
        [Test] public void Loaded() { var data = new SettingsData { soundEnabled = false }; var m = new SettingsModel(data); Assert.That(m.Data, Is.SameAs(data)); Assert.That(m.SoundEnabled, Is.False); }
    }
}
