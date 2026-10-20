using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks nearest-center selection, inclusive radii, and deterministic ties.</summary>
    public class TapResolverTests
    {
        private static Board Single()
        {
            var board = new Board(3, 3);
            board.TryAddArrow(5, 0, 0, Direction.Up, out _);
            return board;
        }

        /// <summary>The exact center selects its arrow.</summary>
        [Test]
        public void ExactCenterResolves()
        {
            Board board = Single();
            Assert.That(TapResolver.Resolve(board, 0, 0, 0.6f), Is.SameAs(board.GetArrowById(5)));
        }

        /// <summary>The gap toward an empty neighbor remains selectable.</summary>
        [Test]
        public void HalfCellTowardEmptyNeighborResolves()
        {
            Board board = Single();
            Assert.That(TapResolver.Resolve(board, 0.5f, 0, 0.6f), Is.SameAs(board.GetArrowById(5)));
        }

        /// <summary>Points just inside and outside the default radius differ.</summary>
        [TestCase(0.59f, true)]
        [TestCase(0.6f, true)]
        [TestCase(0.61f, false)]
        public void RadiusIsInclusive(float x, bool resolves)
        {
            Assert.That(TapResolver.Resolve(Single(), x, 0, 0.6f) != null, Is.EqualTo(resolves));
        }

        /// <summary>The nearest of two neighbors wins.</summary>
        [TestCase(0.4f, 5)]
        [TestCase(0.6f, 2)]
        [TestCase(0.5f, 2)]
        public void NeighborAndTieResolution(float x, int id)
        {
            Board board = Single();
            board.TryAddArrow(2, 1, 0, Direction.Left, out _);
            Assert.That(TapResolver.Resolve(board, x, 0, 0.6f).Id, Is.EqualTo(id));
        }

        /// <summary>Out-of-range and nonfinite points safely miss.</summary>
        [TestCase(-100f, -100f)]
        [TestCase(100000f, 100000f)]
        [TestCase(float.MaxValue, float.MinValue)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void FarPointsReturnNull(float x, float y)
        {
            Assert.That(TapResolver.Resolve(Single(), x, y, 0.6f), Is.Null);
        }

        /// <summary>A small negative coordinate can still be near the edge arrow.</summary>
        [Test]
        public void NegativePointNearEdgeCanResolve()
        {
            Assert.That(TapResolver.Resolve(Single(), -0.5f, 0, 0.6f), Is.Not.Null);
        }

        /// <summary>Absent boards and empty boards select nothing.</summary>
        [Test]
        public void NullAndEmptyBoardsReturnNull()
        {
            Assert.That(TapResolver.Resolve(null, 0, 0, 0.6f), Is.Null);
            Assert.That(TapResolver.Resolve(new Board(2, 2), 0, 0, 0.6f), Is.Null);
        }

        /// <summary>A larger radius can accept a previous miss.</summary>
        [Test]
        public void LargerRadiusAcceptsPreviousMiss()
        {
            Board board = Single();
            Assert.That(TapResolver.Resolve(board, 0.7f, 0, 0.6f), Is.Null);
            Assert.That(TapResolver.Resolve(board, 0.7f, 0, 0.75f), Is.Not.Null);
        }

        /// <summary>A full grid always resolves its exact cell centers correctly.</summary>
        [Test]
        public void DenseBoardCentersResolveToTheirOwnArrows()
        {
            var board = new Board(3, 3);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    board.TryAddArrow(y * 3 + x, x, y, Direction.Up, out _);
            foreach (Arrow arrow in board.Arrows)
                Assert.That(TapResolver.Resolve(board, arrow.X, arrow.Y, 0.6f), Is.SameAs(arrow));
        }

        /// <summary>Selection uses Euclidean distance rather than a square hit area.</summary>
        [Test]
        public void DiagonalOutsideCircleMisses()
        {
            Assert.That(TapResolver.Resolve(Single(), 0.5f, 0.5f, 0.6f), Is.Null);
        }
    }
}
