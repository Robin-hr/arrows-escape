using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks greedy clearing and its non-mutating behavior.</summary>
    public class LevelSolvabilityTests
    {
        private static void Add(Board board, int id, int x, int y, Direction direction)
        {
            Assert.That(board.TryAddArrow(id, x, y, direction, out _), Is.True);
        }

        /// <summary>A single free arrow can clear the board.</summary>
        [Test]
        public void SingleArrowIsSolvable()
        {
            var board = new Board(3, 3);
            Add(board, 1, 1, 1, Direction.Up);
            Assert.That(LevelSolvability.IsSolvable(board), Is.True);
        }

        /// <summary>A blocked chain clears from its front.</summary>
        [Test]
        public void ChainIsSolvable()
        {
            var board = new Board(3, 1);
            Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 1, 0, Direction.Right);
            Add(board, 3, 2, 0, Direction.Right);
            Assert.That(LevelSolvability.IsSolvable(board), Is.True);
        }

        /// <summary>Facing arrows cannot start clearing.</summary>
        [Test]
        public void FacingArrowsAreUnsolvable()
        {
            var board = new Board(2, 1);
            Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 1, 0, Direction.Left);
            Assert.That(LevelSolvability.IsSolvable(board), Is.False);
        }

        /// <summary>A real four-way cycle has no free arrow.</summary>
        [Test]
        public void SquareCycleIsUnsolvable()
        {
            var board = new Board(2, 2);
            // Bottom-left -> bottom-right -> top-right -> top-left -> bottom-left.
            Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 1, 0, Direction.Up);
            Add(board, 3, 1, 1, Direction.Left);
            Add(board, 4, 0, 1, Direction.Down);
            Assert.That(LevelSolvability.CountFreeArrows(board), Is.Zero);
            Assert.That(LevelSolvability.IsSolvable(board), Is.False);
        }

        /// <summary>A solvable board keeps its original instances and order.</summary>
        [Test]
        public void SolverLeavesOriginalUnchanged()
        {
            var board = new Board(2, 1);
            Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 1, 0, Direction.Right);
            Arrow first = board.Arrows[0];
            Arrow second = board.Arrows[1];
            Assert.That(LevelSolvability.IsSolvable(board), Is.True);
            Assert.That(board.Arrows.Count, Is.EqualTo(2));
            Assert.That(board.Arrows[0], Is.SameAs(first));
            Assert.That(board.Arrows[1], Is.SameAs(second));
        }

        /// <summary>Free-arrow counting ignores blocked arrows.</summary>
        [Test]
        public void CountFreeArrowsIsCorrect()
        {
            var board = new Board(3, 2);
            Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 2, 0, Direction.Up);
            Add(board, 3, 1, 1, Direction.Up);
            Assert.That(LevelSolvability.CountFreeArrows(board), Is.EqualTo(2));
        }

        /// <summary>Null boards have no free arrows and are not solvable.</summary>
        [Test]
        public void NullBoardIsHandled()
        {
            Assert.That(LevelSolvability.IsSolvable(null), Is.False);
            Assert.That(LevelSolvability.CountFreeArrows(null), Is.Zero);
        }

        /// <summary>An already cleared board is solvable.</summary>
        [Test]
        public void EmptyBoardIsSolvable()
        {
            var board = new Board(1, 1);
            Assert.That(LevelSolvability.IsSolvable(board), Is.True);
            Assert.That(LevelSolvability.CountFreeArrows(board), Is.Zero);
        }
    }
}
