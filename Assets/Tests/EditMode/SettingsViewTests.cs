using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks preference toggles and confirmation callbacks via actual runtime buttons.</summary>
    public class SettingsViewTests
    {
        private FlowTestScene s;
        /// <summary>Opens settings on a fresh isolated app.</summary>
        [SetUp] public void Setup() { s = new FlowTestScene(); s.Flow.MainMenu.SettingsButton.onClick.Invoke(); }
        /// <summary>Removes isolated objects.</summary>
        [TearDown] public void Cleanup() => s.Dispose();
        /// <summary>Toggle labels, colors, model values, and storage all update together.</summary>
        [Test] public void Toggles()
        {
            var view = s.Flow.SettingsScreen; Color original = view.SoundButton.colors.normalColor;
            view.SoundButton.onClick.Invoke(); Assert.That(s.Bootstrap.Settings.Model.SoundEnabled, Is.False); Assert.That(view.SoundButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Sound: Off")); Assert.That(view.SoundButton.colors.normalColor, Is.Not.EqualTo(original));
            view.HapticsButton.onClick.Invoke(); Assert.That(s.Bootstrap.Settings.Model.HapticsEnabled, Is.False); Assert.That(view.HapticsButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Haptics: Off")); Assert.That(s.SettingsStore.Saves, Is.EqualTo(2));
            s.Bootstrap.Settings.Model.SoundEnabled = true; Assert.That(view.SoundButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Sound: On"));
        }
        /// <summary>Cancel leaves records and save count untouched; Confirm clears and saves.</summary>
        [Test] public void Reset()
        {
            s.Bootstrap.Progress.Tracker.RecordCompletion("level_001", 1, 0); int saves = s.ProgressStore.Saves;
            s.Flow.SettingsScreen.ResetButton.onClick.Invoke(); Assert.That(s.Flow.Confirm.IsOpen, Is.True); s.Flow.Confirm.CancelButton.onClick.Invoke(); Assert.That(s.Bootstrap.Progress.Tracker.IsCompleted("level_001"), Is.True); Assert.That(s.ProgressStore.Saves, Is.EqualTo(saves));
            s.Flow.SettingsScreen.ResetButton.onClick.Invoke(); s.Flow.Confirm.ConfirmButton.onClick.Invoke(); Assert.That(s.Bootstrap.Progress.Tracker.Data.levels, Is.Empty); Assert.That(s.ProgressStore.Saves, Is.EqualTo(saves + 1));
        }
        /// <summary>Back returns to the originating main menu.</summary>
        [Test] public void Back() { s.Flow.SettingsScreen.BackButton.onClick.Invoke(); Assert.That(s.Flow.Machine.Current, Is.EqualTo(ArrowEscape.Core.ScreenState.MainMenu)); }
    }
}
