using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks pentatonic intervals and the repeating melody.</summary>
    public class MusicScaleTests
    {
        /// <summary>Two octaves include their final root.</summary>
        [Test] public void CountAndOffsets()
        {
            Assert.That(MusicScale.NoteCount(2), Is.EqualTo(11));
            Assert.That(MusicScale.Offsets, Is.EqualTo(new[] { 0, 2, 4, 7, 9 }));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<int>)MusicScale.Offsets)[0] = 9);
        }
        /// <summary>Invalid octave counts are rejected.</summary>
        [TestCase(0)][TestCase(-1)] public void InvalidOctaves(int value) => Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.NoteCount(value));
        /// <summary>All eleven notes use the expected pentatonic intervals.</summary>
        [TestCase(0,0)][TestCase(1,2)][TestCase(2,4)][TestCase(3,7)][TestCase(4,9)]
        [TestCase(5,12)][TestCase(6,14)][TestCase(7,16)][TestCase(8,19)][TestCase(9,21)][TestCase(10,24)]
        public void Semitones(int index, int expected) => Assert.That(MusicScale.SemitonesFor(index), Is.EqualTo(expected));
        /// <summary>Pitch classes repeat across later octaves.</summary>
        [Test] public void PitchClasses() { for (int i = 0; i < 100; i++) Assert.That(new[] { 0,2,4,7,9 }, Does.Contain(MusicScale.SemitonesFor(i) % 12)); }
        /// <summary>Roots double each octave and every successive note rises.</summary>
        [Test] public void Frequencies()
        {
            Assert.That(MusicScale.FrequencyHz(0, SoundDesign.RootHz), Is.EqualTo(261.6256).Within(0.01));
            Assert.That(MusicScale.FrequencyHz(5, SoundDesign.RootHz), Is.EqualTo(2 * SoundDesign.RootHz).Within(0.01));
            Assert.That(MusicScale.FrequencyHz(10, SoundDesign.RootHz), Is.EqualTo(4 * SoundDesign.RootHz).Within(0.01));
            for (int i = 1; i <= 10; i++) Assert.That(MusicScale.FrequencyHz(i, SoundDesign.RootHz), Is.GreaterThan(MusicScale.FrequencyHz(i - 1, SoundDesign.RootHz)));
        }
        /// <summary>Negative indices and invalid roots are rejected.</summary>
        [Test] public void InvalidPitch()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.SemitonesFor(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.FrequencyHz(-1, 100));
            foreach (float root in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.FrequencyHz(0, root));
        }
        /// <summary>At least two complete periods have no duplicated endpoints.</summary>
        [Test] public void PingPong()
        {
            int[] period = { 0,1,2,3,4,5,6,7,8,9,10,9,8,7,6,5,4,3,2,1 };
            for (int i = 0; i < 65; i++) Assert.That(MusicScale.PingPongIndex(i, 11), Is.EqualTo(period[i % 20]));
            Assert.That(MusicScale.PingPongIndex(-5, 11), Is.Zero);
            Assert.That(MusicScale.PingPongIndex(int.MaxValue, 1), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.PingPongIndex(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicScale.PingPongIndex(0, -1));
        }
    }
}
