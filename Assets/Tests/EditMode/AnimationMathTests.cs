using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks curve endpoints, ranges, and direction distances with no graphics.</summary>
    public class AnimationMathTests
    {
        /// <summary>Each easing curve is clamped, monotonic, and has exact endpoints.</summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EasingIsClampedAndMonotonic(int curve)
        {
            Func<float, float> ease = curve == 0 ? AnimationMath.EaseInQuad :
                curve == 1 ? AnimationMath.EaseOutCubic : AnimationMath.EaseInOutQuad;
            Assert.That(ease(-1), Is.Zero);
            Assert.That(ease(0), Is.Zero);
            Assert.That(ease(1), Is.EqualTo(1));
            Assert.That(ease(2), Is.EqualTo(1));
            float previous = 0;
            for (int i = 0; i <= 1000; i++)
            {
                float value = ease(i / 1000f);
                Assert.That(value, Is.InRange(previous, 1));
                previous = value;
            }
        }

        /// <summary>Distance includes the complete forward path plus the outside span.</summary>
        [TestCase(4, 4, 0, 3, Direction.Up, 1.5f)]
        [TestCase(4, 4, 0, 0, Direction.Down, 1.5f)]
        [TestCase(4, 4, 0, 0, Direction.Left, 1.5f)]
        [TestCase(4, 4, 3, 0, Direction.Right, 1.5f)]
        [TestCase(4, 4, 1, 1, Direction.Up, 3.5f)]
        [TestCase(4, 4, 1, 1, Direction.Down, 2.5f)]
        [TestCase(4, 4, 1, 1, Direction.Left, 2.5f)]
        [TestCase(4, 4, 1, 1, Direction.Right, 3.5f)]
        [TestCase(3, 7, 1, 2, Direction.Up, 5.5f)]
        [TestCase(3, 7, 1, 2, Direction.Down, 3.5f)]
        [TestCase(3, 7, 1, 2, Direction.Left, 2.5f)]
        [TestCase(3, 7, 1, 2, Direction.Right, 2.5f)]
        public void SlideDistanceMatchesBoard(int width, int height, int x, int y, Direction direction, float expected)
        {
            Assert.That(AnimationMath.SlideDistance(width, height, x, y, direction), Is.EqualTo(expected));
        }

        /// <summary>Slide fading remains opaque until its threshold, then decreases to zero.</summary>
        [TestCase(0.7f)]
        [TestCase(0f)]
        [TestCase(0.9f)]
        public void SlideAlphaFadesMonotonically(float fadeStart)
        {
            Assert.That(AnimationMath.SlideAlpha(0, fadeStart), Is.EqualTo(1));
            Assert.That(AnimationMath.SlideAlpha(fadeStart, fadeStart), Is.EqualTo(1));
            Assert.That(AnimationMath.SlideAlpha(1, fadeStart), Is.Zero);
            float previous = 1;
            for (int i = 0; i <= 1000; i++)
            {
                float value = AnimationMath.SlideAlpha(i / 1000f, fadeStart);
                Assert.That(value, Is.InRange(0, previous));
                previous = value;
            }
        }

        /// <summary>The bump is nonnegative and returns exactly to its starting point.</summary>
        [Test]
        public void BumpHasExactEndpointsAndPositivePeak()
        {
            Assert.That(AnimationMath.BumpOffset(0, 0.18f), Is.Zero);
            Assert.That(AnimationMath.BumpOffset(1, 0.18f), Is.Zero);
            Assert.That(AnimationMath.BumpOffset(0.35f, 0.18f), Is.EqualTo(0.18f));
            for (int i = 0; i <= 1000; i++)
                Assert.That(AnimationMath.BumpOffset(i / 1000f, 0.18f), Is.InRange(0, 0.18f));
        }

        /// <summary>The pulse grows above one and restores exactly one.</summary>
        [Test]
        public void PulseHasExactEndpointsAndPeak()
        {
            Assert.That(AnimationMath.PulseScale(0, 1.15f), Is.EqualTo(1));
            Assert.That(AnimationMath.PulseScale(1, 1.15f), Is.EqualTo(1));
            Assert.That(AnimationMath.PulseScale(0.4f, 1.15f), Is.EqualTo(1.15f));
            for (int i = 0; i <= 1000; i++)
                Assert.That(AnimationMath.PulseScale(i / 1000f, 1.15f), Is.InRange(1, 1.15f));
        }
    }
}
