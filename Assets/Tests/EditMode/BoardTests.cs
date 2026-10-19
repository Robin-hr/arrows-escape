using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks grid rules without scenes or play mode.</summary>
    public class BoardTests
    {
        private static Arrow Add(Board board, int id, int x, int y, Direction direction)
        {
            Assert.That(board.TryAddArrow(id, x, y, direction, out Arrow arrow), Is.True);
            return arrow;
        }

        /// <summary>Both dimensions must be positive.</summary>
        [TestCase(0, 3)]
        [TestCase(-1, 3)]
        [TestCase(3, 0)]
        [TestCase(3, -1)]
        public void ConstructorRejectsInvalidDimensions(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(width, height));
        }

        /// <summary>Valid additions preserve their data and insertion order.</summary>
        [Test]
        public void TryAddArrowSucceeds()
        {
            var board = new Board(3, 4);
            Arrow first = Add(board, 8, 1, 2, Direction.Left);
            Arrow second = Add(board, 2, 0, 0, Direction.Up);
            Assert.That(board.Width, Is.EqualTo(3));
            Assert.That(board.Height, Is.EqualTo(4));
            Assert.That(first.Id, Is.EqualTo(8));
            Assert.That(first.X, Is.EqualTo(1));
            Assert.That(first.Y, Is.EqualTo(2));
            Assert.That(first.Direction, Is.EqualTo(Direction.Left));
            Assert.That(board.Arrows, Is.EqualTo(new[] { first, second }));
            Assert.That(board.GetArrowAt(1, 2), Is.SameAs(first));
            Assert.That(board.GetArrowById(8), Is.SameAs(first));
        }

        /// <summary>All four bounds reject additions without returning an arrow.</summary>
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 3)]
        public void TryAddArrowRejectsOutOfBounds(int x, int y)
        {
            var board = new Board(3, 3);
            Assert.That(board.TryAddArrow(1, x, y, Direction.Up, out Arrow arrow), Is.False);
            Assert.That(arrow, Is.Null);
            Assert.That(board.InBounds(x, y), Is.False);
            Assert.That(board.GetArrowAt(x, y), Is.Null);
            Assert.That(board.IsCleared(), Is.True);
        }

        /// <summary>Occupied cells and duplicate identifiers leave the board unchanged.</summary>
        [Test]
        public void TryAddArrowRejectsOccupiedCellAndDuplicateId()
        {
            var board = new Board(3, 3);
            Add(board, 1, 0, 0, Direction.Up);
            Assert.That(board.TryAddArrow(2, 0, 0, Direction.Left, out Arrow occupied), Is.False);
            Assert.That(occupied, Is.Null);
            Assert.That(board.TryAddArrow(1, 1, 1, Direction.Right, out Arrow duplicate), Is.False);
            Assert.That(duplicate, Is.Null);
            Assert.That(board.Arrows.Count, Is.EqualTo(1));
        }

        /// <summary>An otherwise empty path is clear in every direction.</summary>
        [TestCase(Direction.Up)]
        [TestCase(Direction.Down)]
        [TestCase(Direction.Left)]
        [TestCase(Direction.Right)]
        public void CanExitWithEmptyPath(Direction direction)
        {
            var board = new Board(5, 5);
            Arrow arrow = Add(board, 1, 2, 2, direction);
            Assert.That(board.CanExit(arrow), Is.True);
            Assert.That(board.GetBlocker(arrow), Is.Null);
        }

        /// <summary>Every outward-facing edge exits immediately.</summary>
        [TestCase(2, 4, Direction.Up)]
        [TestCase(2, 0, Direction.Down)]
        [TestCase(0, 2, Direction.Left)]
        [TestCase(4, 2, Direction.Right)]
        public void EdgeArrowCanExit(int x, int y, Direction direction)
        {
            var board = new Board(5, 5);
            Assert.That(board.CanExit(Add(board, 1, x, y, direction)), Is.True);
        }

        /// <summary>Adjacent and distant obstacles block all four directions.</summary>
        [TestCase(Direction.Up, 1)]
        [TestCase(Direction.Down, 1)]
        [TestCase(Direction.Left, 1)]
        [TestCase(Direction.Right, 1)]
        [TestCase(Direction.Up, 3)]
        [TestCase(Direction.Down, 3)]
        [TestCase(Direction.Left, 3)]
        [TestCase(Direction.Right, 3)]
        public void CanExitRejectsBlocker(Direction direction, int distance)
        {
            var board = new Board(7, 7);
            Arrow arrow = Add(board, 1, 3, 3, direction);
            Add(board, 2, 3 + direction.GetDx() * distance,
                3 + direction.GetDy() * distance, direction.Opposite());
            Assert.That(board.CanExit(arrow), Is.False);
        }

        /// <summary>Only cells ahead in the same line matter.</summary>
        [TestCase(Direction.Up)]
        [TestCase(Direction.Down)]
        [TestCase(Direction.Left)]
        [TestCase(Direction.Right)]
        public void BehindAndBesideDoNotBlock(Direction direction)
        {
            var board = new Board(5, 5);
            Arrow arrow = Add(board, 1, 2, 2, direction);
            Add(board, 2, 2 - direction.GetDx(), 2 - direction.GetDy(), direction);
            Add(board, 3, 2 + direction.GetDy(), 2 + direction.GetDx(), direction);
            Assert.That(board.CanExit(arrow), Is.True);
        }

        /// <summary>Distance, rather than insertion order, determines the blocker.</summary>
        [Test]
        public void GetBlockerReturnsNearest()
        {
            var board = new Board(6, 1);
            Arrow arrow = Add(board, 1, 0, 0, Direction.Right);
            Add(board, 2, 5, 0, Direction.Left);
            Arrow nearest = Add(board, 3, 2, 0, Direction.Up);
            Assert.That(board.GetBlocker(arrow), Is.SameAs(nearest));
        }

        /// <summary>Removal is allowed even when blocked and frees arrows behind it.</summary>
        [Test]
        public void RemoveFreesPreviouslyBlockedArrow()
        {
            var board = new Board(3, 1);
            Arrow arrow = Add(board, 1, 0, 0, Direction.Right);
            Arrow blocker = Add(board, 2, 1, 0, Direction.Left);
            Assert.That(board.CanExit(arrow), Is.False);
            Assert.That(board.CanExit(blocker), Is.False);
            Assert.That(board.Remove(blocker), Is.True);
            Assert.That(board.CanExit(arrow), Is.True);
            Assert.That(board.GetArrowById(2), Is.Null);
            Assert.That(board.GetArrowAt(1, 0), Is.Null);
        }

        /// <summary>Null, foreign, and removed arrows are handled safely.</summary>
        [Test]
        public void NonmembersCannotExitBlockOrBeRemoved()
        {
            var board = new Board(2, 2);
            Arrow member = Add(board, 1, 0, 0, Direction.Up);
            Arrow foreign = new Arrow(1, 0, 0, Direction.Up);
            foreach (Arrow arrow in new[] { null, foreign })
            {
                Assert.That(board.CanExit(arrow), Is.False);
                Assert.That(board.GetBlocker(arrow), Is.Null);
                Assert.That(board.Remove(arrow), Is.False);
            }
            board.Remove(member);
            Assert.That(board.Remove(member), Is.False);
            Assert.That(board.CanExit(member), Is.False);
            Assert.That(board.GetBlocker(member), Is.Null);
        }

        /// <summary>The board clears only after the final arrow is removed.</summary>
        [Test]
        public void IsClearedTracksRemainingArrows()
        {
            var board = new Board(2, 1);
            Assert.That(board.IsCleared(), Is.True);
            Arrow first = Add(board, 1, 0, 0, Direction.Up);
            Arrow second = Add(board, 2, 1, 0, Direction.Up);
            Assert.That(board.IsCleared(), Is.False);
            board.Remove(first);
            Assert.That(board.IsCleared(), Is.False);
            board.Remove(second);
            Assert.That(board.IsCleared(), Is.True);
        }

        /// <summary>Removing the front of a chain unlocks each arrow in turn.</summary>
        [Test]
        public void ChainClearsFromFrontToBack()
        {
            var board = new Board(3, 1);
            Arrow a = Add(board, 1, 0, 0, Direction.Right);
            Arrow b = Add(board, 2, 1, 0, Direction.Right);
            Arrow c = Add(board, 3, 2, 0, Direction.Right);
            Assert.That(board.GetBlocker(a), Is.SameAs(b));
            Assert.That(board.GetBlocker(b), Is.SameAs(c));
            Assert.That(board.CanExit(c), Is.True);
            board.Remove(c);
            Assert.That(board.CanExit(b), Is.True);
            Assert.That(board.CanExit(a), Is.False);
            board.Remove(b);
            Assert.That(board.CanExit(a), Is.True);
            board.Remove(a);
            Assert.That(board.IsCleared(), Is.True);
        }

        /// <summary>Clones preserve data and order but share no arrow instances.</summary>
        [Test]
        public void CloneIsIndependent()
        {
            var original = new Board(4, 3);
            Add(original, 9, 1, 1, Direction.Right);
            Add(original, 3, 3, 1, Direction.Up);
            Board clone = original.Clone();
            Assert.That(clone.Width, Is.EqualTo(original.Width));
            Assert.That(clone.Height, Is.EqualTo(original.Height));
            for (int i = 0; i < original.Arrows.Count; i++)
            {
                Arrow source = original.Arrows[i];
                Arrow copy = clone.Arrows[i];
                Assert.That(copy, Is.Not.SameAs(source));
                Assert.That(copy.Id, Is.EqualTo(source.Id));
                Assert.That(copy.X, Is.EqualTo(source.X));
                Assert.That(copy.Y, Is.EqualTo(source.Y));
                Assert.That(copy.Direction, Is.EqualTo(source.Direction));
            }
            clone.Remove(clone.GetArrowById(3));
            Assert.That(clone.CanExit(clone.GetArrowById(9)), Is.True);
            Assert.That(original.Arrows.Count, Is.EqualTo(2));
            Assert.That(original.CanExit(original.GetArrowById(9)), Is.False);
        }

        /// <summary>Arrows facing each other both have blocked paths.</summary>
        [Test]
        public void FacingArrowsCannotExit()
        {
            var board = new Board(5, 1);
            Arrow left = Add(board, 1, 1, 0, Direction.Right);
            Arrow right = Add(board, 2, 3, 0, Direction.Left);
            Assert.That(board.CanExit(left), Is.False);
            Assert.That(board.CanExit(right), Is.False);
        }

        /// <summary>Direction helpers follow the documented coordinate system.</summary>
        [TestCase(Direction.Up, 0, 1, Direction.Down)]
        [TestCase(Direction.Down, 0, -1, Direction.Up)]
        [TestCase(Direction.Left, -1, 0, Direction.Right)]
        [TestCase(Direction.Right, 1, 0, Direction.Left)]
        public void DirectionHelpersMatchCoordinates(Direction direction, int dx, int dy, Direction opposite)
        {
            Assert.That(direction.GetDx(), Is.EqualTo(dx));
            Assert.That(direction.GetDy(), Is.EqualTo(dy));
            Assert.That(direction.Opposite(), Is.EqualTo(opposite));
        }
    }
}
