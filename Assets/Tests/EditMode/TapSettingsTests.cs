using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks immutable, finite input distances.</summary>
    public class TapSettingsTests
    {
        /// <summary>The defaults use forgiving selection and a larger release radius.</summary>
        [Test]
        public void DefaultsMatchDesign()
        {
            var settings = new TapSettings();
            Assert.That(settings.SelectRadius, Is.EqualTo(0.6f));
            Assert.That(settings.CancelRadius, Is.EqualTo(0.8f));
        }

        /// <summary>Equal positive radii are valid.</summary>
        [Test]
        public void EqualRadiiAreAllowed()
        {
            var settings = new TapSettings(1, 1);
            Assert.That(settings.SelectRadius, Is.EqualTo(1));
            Assert.That(settings.CancelRadius, Is.EqualTo(1));
        }

        /// <summary>Zero, negative, nonfinite, and inverted settings fail clearly.</summary>
        [TestCase(0f, 0.8f)]
        [TestCase(-1f, 0.8f)]
        [TestCase(float.NaN, 0.8f)]
        [TestCase(float.PositiveInfinity, 0.8f)]
        [TestCase(0.6f, 0.59f)]
        [TestCase(0.6f, -1f)]
        [TestCase(0.6f, float.NaN)]
        [TestCase(0.6f, float.PositiveInfinity)]
        public void InvalidSettingsThrow(float select, float cancel)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TapSettings(select, cancel));
        }
    }
}
