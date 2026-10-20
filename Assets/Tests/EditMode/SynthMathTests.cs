using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Measures actual synthesized samples without an audio device.</summary>
    public class SynthMathTests
    {
        private const int Rate = SoundDesign.SampleRate;
        private static double Energy(float[] data, double hz)
        {
            // Goertzel evaluates energy at one frequency without a spectrum package.
            double coefficient = 2 * Math.Cos(2 * Math.PI * hz / Rate), s1 = 0, s2 = 0;
            foreach (float sample in data) { double s = sample + coefficient * s1 - s2; s2 = s1; s1 = s; }
            return s1 * s1 + s2 * s2 - coefficient * s1 * s2;
        }
        private static double Rms(float[] data, int start, int count)
        { double sum = 0; for (int i = start; i < start + count; i++) sum += data[i] * data[i]; return Math.Sqrt(sum / count); }
        private static void Healthy(float[] data)
        {
            float peak = 0;
            foreach (float value in data) { Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False); peak = Math.Max(peak, Math.Abs(value)); }
            Assert.That(peak, Is.InRange(.75f,.85f)); Assert.That(data[data.Length - 1], Is.Zero);
        }
        /// <summary>Length, smooth edges, normalized peak, decay, and finite output.</summary>
        [Test] public void NoteEnvelope()
        {
            float[] data = SynthMath.RenderNote(SoundDesign.RootHz, .55f, Rate);
            Assert.That(data.Length, Is.EqualTo((int)Math.Round((double).55f * Rate))); Assert.That(Math.Abs(data[0]), Is.LessThan(.01)); Healthy(data);
            Assert.That(Rms(data, 0, data.Length / 4), Is.GreaterThan(Rms(data, data.Length * 3 / 4, data.Length / 4)));
        }
        /// <summary>The fundamental dominates unrelated low and high frequencies.</summary>
        [Test] public void NotePitch()
        {
            float hz = SoundDesign.RootHz; float[] data = SynthMath.RenderNote(hz,.55f,Rate);
            Assert.That(Energy(data,hz), Is.GreaterThan(10 * Energy(data,hz * 1.5))); Assert.That(Energy(data,hz), Is.GreaterThan(10 * Energy(data,hz * .5)));
        }
        /// <summary>Identical inputs produce byte-for-byte equal floats.</summary>
        [Test] public void Deterministic()
        {
            Assert.That(SynthMath.RenderNote(300,.1f,Rate), Is.EqualTo(SynthMath.RenderNote(300,.1f,Rate)));
            Assert.That(SynthMath.RenderNote(300,.1f,Rate), Is.Not.EqualTo(SynthMath.RenderNote(400,.1f,Rate)));
            Assert.That(SynthMath.RenderThud(.18f,Rate), Is.EqualTo(SynthMath.RenderThud(.18f,Rate)));
        }
        /// <summary>Invalid oscillator and buffer inputs are rejected.</summary>
        [TestCase(0,.1f,44100)][TestCase(-1,.1f,44100)][TestCase(22050,.1f,44100)][TestCase(24000,.1f,44100)]
        [TestCase(300,0,44100)][TestCase(300,.1f,4000)][TestCase(float.NaN,.1f,44100)][TestCase(300,float.PositiveInfinity,44100)]
        public void InvalidNote(float hz, float seconds, int rate) => Assert.Throws<ArgumentOutOfRangeException>(() => SynthMath.RenderNote(hz,seconds,rate));
        /// <summary>The thud has low-frequency energy and the same safe envelope.</summary>
        [Test] public void Thud()
        {
            float[] data = SynthMath.RenderThud(.18f,Rate); Healthy(data);
            Assert.That(data.Length, Is.EqualTo((int)Math.Round((double).18f * Rate)));
            Assert.That(Energy(data,100), Is.GreaterThan(5 * Energy(data,1000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => SynthMath.RenderThud(0,Rate));
            Assert.Throws<ArgumentOutOfRangeException>(() => SynthMath.RenderThud(.1f,4000));
        }
        /// <summary>Overlapping notes normalize and truncate, including a negative start.</summary>
        [Test] public void Mix()
        {
            var notes = new[] { new NoteSpec(300,0,.55f), new NoteSpec(300,0,.55f), new NoteSpec(400,.1f,1), new NoteSpec(500,-.1f,.3f), new NoteSpec(600,100,.2f) };
            float[] data = SynthMath.RenderMix(notes,.3f,Rate); Healthy(data);
            Assert.That(data, Is.EqualTo(SynthMath.RenderMix(notes,.3f,Rate)));
            float[] empty = SynthMath.RenderMix(Array.Empty<NoteSpec>(),.1f,Rate);
            Assert.That(empty.Length, Is.EqualTo((int)Math.Round((double).1f * Rate))); Assert.That(empty, Is.All.Zero);
            Assert.Throws<ArgumentNullException>(() => SynthMath.RenderMix(null,.1f,Rate));
        }
        /// <summary>The exact arpeggio timeline includes silence before its first note.</summary>
        [Test] public void Jingle()
        {
            NoteSpec[] notes = SoundDesign.CreateJingle(); int[] steps = { 5,7,8,9,10 };
            for (int i = 0; i < notes.Length; i++)
            {
                Assert.That(notes[i].FrequencyHz, Is.EqualTo(MusicScale.FrequencyHz(steps[i],SoundDesign.RootHz)));
                Assert.That(notes[i].StartSeconds, Is.EqualTo(.15f + .11f * i).Within(.000001));
                Assert.That(notes[i].DurationSeconds, Is.EqualTo(i == 4 ? .9f : .5f));
            }
            float[] data = SynthMath.RenderMix(notes,SoundDesign.JingleTotalSeconds,Rate);
            Assert.That(data.Length, Is.EqualTo((int)Math.Round((double)SoundDesign.JingleTotalSeconds * Rate)));
            for (int i = 0; i < (.15f - .001f) * Rate; i++) Assert.That(Math.Abs(data[i]), Is.LessThan(.0001));
            Assert.That(Rms(data,(int)(.2f * Rate),(int)(.4f * Rate)), Is.GreaterThan(.02)); Healthy(data);
        }
    }
}
