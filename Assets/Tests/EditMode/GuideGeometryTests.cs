using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks all directions, distances, and invalid connector inputs.</summary>
    public class GuideGeometryTests
    {
        /// <summary>All cardinal directions produce inset endpoints and the correct length.</summary>
        [TestCase(Direction.Up, 1)][TestCase(Direction.Up, 2)][TestCase(Direction.Up, 5)]
        [TestCase(Direction.Down, 1)][TestCase(Direction.Down, 2)][TestCase(Direction.Down, 5)]
        [TestCase(Direction.Left, 1)][TestCase(Direction.Left, 2)][TestCase(Direction.Left, 5)]
        [TestCase(Direction.Right, 1)][TestCase(Direction.Right, 2)][TestCase(Direction.Right, 5)]
        public void Directions(Direction direction, int distance)
        {
            int dx = direction.GetDx(), dy = direction.GetDy(); var a = new Arrow(1, 2, 2, direction); var b = new Arrow(2, 2 + dx * distance, 2 + dy * distance, Direction.Up);
            Assert.That(GuideGeometry.TryComputeSegment(a, b, 0.35f, out var segment), Is.True);
            Assert.That(segment.StartX, Is.EqualTo(2 + dx * 0.35f).Within(0.00001)); Assert.That(segment.StartY, Is.EqualTo(2 + dy * 0.35f).Within(0.00001));
            Assert.That(segment.EndX, Is.EqualTo(b.X - dx * 0.35f).Within(0.00001)); Assert.That(segment.EndY, Is.EqualTo(b.Y - dy * 0.35f).Within(0.00001));
            Assert.That(segment.Length, Is.EqualTo(distance - 0.7f).Within(0.00001)); Assert.That(segment.IsHorizontal, Is.EqualTo(dx != 0));
        }
        /// <summary>Rejects missing, non-forward, diagonal, and invalid-inset requests.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)]
        public void Invalid(int kind)
        {
            Arrow a = new Arrow(1, 2, 2, Direction.Up), b = new Arrow(2, 2, 3, Direction.Up); float inset = 0.35f;
            if (kind == 0) a = null; if (kind == 1) b = null; if (kind == 2) b = new Arrow(2, 2, 1, Direction.Up); if (kind == 3) b = new Arrow(2, 3, 2, Direction.Up);
            if (kind == 4) b = new Arrow(2, 3, 3, Direction.Up); if (kind == 5) b = new Arrow(2, 2, 2, Direction.Up); if (kind == 6) inset = 0; if (kind == 7) inset = 0.5f; if (kind == 8) inset = -1; if (kind == 9) inset = float.NaN; if (kind == 10) inset = float.PositiveInfinity;
            Assert.That(GuideGeometry.TryComputeSegment(a, b, inset, out var result), Is.False); Assert.That(result, Is.EqualTo(default(GuideSegment)));
        }
        /// <summary>The explicit example and adjacent-arrow gap remain positive.</summary>
        [Test] public void Example() { var a = new Arrow(1, 2, 2, Direction.Up); GuideGeometry.TryComputeSegment(a, new Arrow(2, 2, 5, Direction.Left), 0.35f, out var s); Assert.That(s.StartY, Is.EqualTo(2.35f)); Assert.That(s.EndY, Is.EqualTo(4.65f)); Assert.That(s.Length, Is.EqualTo(2.3f).Within(0.00001)); GuideGeometry.TryComputeSegment(a, new Arrow(3, 2, 3, Direction.Left), 0.35f, out s); Assert.That(s.Length, Is.EqualTo(0.3f).Within(0.00001)); }
    }
}
