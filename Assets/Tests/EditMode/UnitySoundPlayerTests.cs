using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Exercises pooling and safe Edit Mode behavior without playing actual audio.</summary>
    public class UnitySoundPlayerTests
    {
        private GameObject host; private UnitySoundPlayer player;
        /// <summary>Creates the six voices once.</summary>
        [SetUp] public void Setup() { host = new GameObject("Sound voices"); player = host.AddComponent<UnitySoundPlayer>(); }
        /// <summary>Destroys sources and clips.</summary>
        [TearDown] public void Cleanup() => Object.DestroyImmediate(host);
        /// <summary>Every source is configured as a non-looping 2D effect.</summary>
        [Test] public void Pool()
        {
            var sources = host.GetComponentsInChildren<AudioSource>(); Assert.That(sources.Length, Is.EqualTo(6)); Assert.That(host.transform.childCount, Is.EqualTo(6));
            foreach (AudioSource source in sources)
            { Assert.That(source.playOnAwake || source.loop, Is.False); Assert.That(source.spatialBlend, Is.Zero); Assert.That(source.bypassEffects, Is.True); Assert.That(source.priority, Is.EqualTo(128)); }
        }
        /// <summary>Rapid requests reuse existing voices, even with missing/disabled sources.</summary>
        [Test] public void RapidPlay()
        {
            for (int i = 0; i < 50; i++) Assert.DoesNotThrow(() => player.Play(SoundPlanner.ForExit(i + 1)));
            Assert.That(host.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(6));
            var sources = host.GetComponentsInChildren<AudioSource>(); foreach (var source in sources) source.enabled = false;
            for (int i = 0; i < 50; i++) Assert.DoesNotThrow(() => player.Play(SoundPlanner.ForExit(i + 1)));
            Assert.That(host.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(6));
            foreach (var source in sources) { Assert.That(source.pitch, Is.EqualTo(1)); Assert.That(source.volume, Is.EqualTo(.6f)); }
        }
        /// <summary>Stopping and a null-mapped cue are harmless.</summary>
        [Test] public void StopAndNullClip()
        {
            Assert.DoesNotThrow(() => player.Play(new SoundEvent((SoundCue)100,-1,1)));
            Assert.DoesNotThrow(player.StopAll);
            foreach (var source in host.GetComponentsInChildren<AudioSource>()) Assert.That(source.clip, Is.Null);
            Object.DestroyImmediate(host.transform.GetChild(0).gameObject); Assert.DoesNotThrow(() => player.Play(SoundPlanner.ForBlocked())); Assert.DoesNotThrow(player.StopAll);
        }
    }
}
