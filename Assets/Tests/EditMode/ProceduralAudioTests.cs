using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Checks real PCM AudioClips; playback hardware is never used.</summary>
    public class ProceduralAudioTests
    {
        private ProceduralAudio audio;
        /// <summary>Creates an empty lazy cache.</summary>
        [SetUp] public void Setup() => audio = new ProceduralAudio();
        /// <summary>Destroys cached clips.</summary>
        [TearDown] public void Cleanup() => audio.Dispose();
        private static void Check(AudioClip clip, float[] expected)
        {
            Assert.That(clip, Is.Not.Null,"AudioClip creation should not require a playback device.");
            Assert.That(clip.samples, Is.EqualTo(expected.Length)); Assert.That(clip.frequency, Is.EqualTo(44100)); Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
            var actual = new float[clip.samples]; Assert.That(clip.GetData(actual,0), Is.True);
            for (int i = 0; i < actual.Length; i++) Assert.That(actual[i], Is.EqualTo(expected[i]).Within(.0001));
        }
        /// <summary>All eleven note clips contain exactly the pure renderer's samples.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)]
        public void NoteSamples(int step)
        { Check(audio.GetClip(new SoundEvent(SoundCue.Exit,step,.6f)),SynthMath.RenderNote(MusicScale.FrequencyHz(step,SoundDesign.RootHz),SoundDesign.NoteSeconds,SoundDesign.SampleRate)); }
        /// <summary>Thud and jingle match the pure generated data, including lead silence.</summary>
        [Test] public void OtherClips()
        {
            Check(audio.GetClip(SoundPlanner.ForBlocked()),GameSounds.WrongBuzz(SoundDesign.SampleRate));
            Check(audio.GetClip(SoundPlanner.ForLose()),GameSounds.LoseMusic(SoundDesign.SampleRate));
            Check(audio.GetClip(SoundPlanner.ForUi(UiFeedbackCue.Purchase)),GameSounds.CashRegister(SoundDesign.SampleRate));
            Check(audio.GetClip(SoundPlanner.ForLevelComplete()),VictoryMusic.Render(SoundDesign.SampleRate));
        }
        /// <summary>Tonal cues share cached clips and clamp steps.</summary>
        [Test] public void Mapping()
        {
            AudioClip first = audio.GetClip(SoundPlanner.ForExit(1));
            foreach (SoundCue cue in new[] { SoundCue.Exit,SoundCue.Undo,SoundCue.Hint,SoundCue.Preview })
                Assert.That(audio.GetClip(new SoundEvent(cue,-99,.5f)), Is.SameAs(first));
            AudioClip last = audio.GetClip(new SoundEvent(SoundCue.Exit,10,1));
            Assert.That(audio.GetClip(new SoundEvent(SoundCue.Exit,100,1)), Is.SameAs(last));
            Assert.That(audio.GetClip(SoundPlanner.ForHint()), Is.SameAs(audio.GetClip(new SoundEvent(SoundCue.Exit,8,1))));
            Assert.That(audio.GetClip(SoundPlanner.ForPreview()), Is.SameAs(audio.GetClip(new SoundEvent(SoundCue.Exit,7,1))));
            Assert.That(audio.GetClip(new SoundEvent((SoundCue)99,0,1)), Is.Null);
            Assert.That(audio.GetClip(SoundPlanner.ForBlocked()), Is.SameAs(audio.GetClip(SoundPlanner.ForBlocked())));
            Assert.That(audio.GetClip(SoundPlanner.ForLevelComplete()), Is.SameAs(audio.GetClip(SoundPlanner.ForLevelComplete())));
        }
        /// <summary>A destroyed cached object is safely recreated for every clip kind.</summary>
        [TestCase(SoundCue.Exit)][TestCase(SoundCue.Blocked)][TestCase(SoundCue.LevelComplete)]
        public void DestroyedCache(SoundCue cue)
        {
            var sound = new SoundEvent(cue,3,.5f); AudioClip old = audio.GetClip(sound); UnityEngine.Object.DestroyImmediate(old);
            AudioClip fresh = audio.GetClip(sound); Assert.That(fresh != null, Is.True); Assert.That(ReferenceEquals(fresh,old), Is.False);
        }
        /// <summary>Disposal destroys every generated object and still permits later recovery.</summary>
        [Test] public void DisposeAndRecover()
        {
            var clips = new AudioClip[13];
            for (int i = 0; i < 11; i++) clips[i] = audio.GetClip(new SoundEvent(SoundCue.Exit,i,1));
            clips[11] = audio.GetClip(SoundPlanner.ForBlocked()); clips[12] = audio.GetClip(SoundPlanner.ForLevelComplete());
            audio.Dispose(); foreach (AudioClip clip in clips) Assert.That(clip == null, Is.True);
            Assert.DoesNotThrow(audio.Dispose); Assert.That(audio.GetClip(SoundPlanner.ForPreview()) != null, Is.True);
        }
    }
}
