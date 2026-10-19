using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks the upward-facing sprite's y-up rotation convention.</summary>
    public class ArrowRotationTests
    {
        /// <summary>Each direction maps to its specified Unity z angle.</summary>
        [TestCase(Direction.Up, 0f)]
        [TestCase(Direction.Left, 90f)]
        [TestCase(Direction.Down, 180f)]
        [TestCase(Direction.Right, -90f)]
        public void DirectionHasExpectedAngle(Direction direction, float expected)
        {
            Assert.That(ArrowRotation.GetRotationZ(direction), Is.EqualTo(expected));
        }
    }
}
