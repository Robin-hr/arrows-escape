using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks responsive menu scaling without screen APIs.</summary>
    public class UiFitTests
    {
        /// <summary>The tighter dimension shrinks content without enlarging it.</summary>
        [TestCase(100, 100, 200, 200, 1)][TestCase(400, 100, 200, 200, 0.5f)][TestCase(100, 400, 200, 200, 0.5f)][TestCase(400, 800, 200, 200, 0.25f)][TestCase(0, 100, 200, 200, 1)][TestCase(-1, 100, 200, 200, 1)][TestCase(100, -1, 200, 200, 1)]
        public void Scale(float w, float h, float aw, float ah, float expected) => Assert.That(UiFit.ComputeScale(w, h, aw, ah), Is.EqualTo(expected));
        /// <summary>Available dimensions must be positive.</summary>
        [TestCase(0, 100)][TestCase(-1, 100)][TestCase(100, 0)][TestCase(100, -1)] public void Invalid(float w, float h) => Assert.Throws<ArgumentOutOfRangeException>(() => UiFit.ComputeScale(100, 100, w, h));
    }
}
