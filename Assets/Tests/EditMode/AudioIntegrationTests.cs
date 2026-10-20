using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Connects real preference services to audio without touching player saves.</summary>
    public class AudioIntegrationTests
    {
        /// <summary>The service's Sound setter immediately silences and previews the director.</summary>
        [Test] public void ServiceToggle()
        {
            var store = new FakeSettingsStore(); var service = new SettingsService(store); service.Load();
            using (var scene = new AudioTestScene(settings:service.Model))
            {
                int stops = scene.Player.Stops; service.SetSound(false); Assert.That(scene.Player.Stops, Is.EqualTo(stops + 1));
                scene.TapId(1); Assert.That(scene.Player.Events, Is.Empty); service.SetSound(true);
                Assert.That(scene.Player.Events.Count, Is.EqualTo(1)); Assert.That(scene.Player.Events[0].Cue, Is.EqualTo(SoundCue.Preview));
                Assert.That(store.Saves, Is.EqualTo(2));
            }
        }
        /// <summary>A saved off setting is respected on the very first gameplay event.</summary>
        [Test] public void SavedOff()
        {
            var service = new SettingsService(new FakeSettingsStore { Saved = new SettingsData { soundEnabled = false } }); service.Load();
            using (var scene = new AudioTestScene(settings:service.Model))
            { scene.TapId(1); Assert.That(scene.Player.Events, Is.Empty); }
        }
        /// <summary>Bootstrap components created in tests never automatically get audio objects.</summary>
        [Test] public void TestBootstrapDoesNotCreateDirector()
        {
            var host = new GameObject("Bootstrap without audio");
            try { host.AddComponent<GameBootstrap>(); Assert.That(host.GetComponent<AudioDirector>(), Is.Null); Assert.That(host.GetComponentsInChildren<AudioSource>(), Is.Empty); }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
