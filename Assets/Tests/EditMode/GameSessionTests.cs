using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks instant logical moves without animations or frame timing.</summary>
    public class GameSessionTests
    {
        private static Board Chain()
        {
            var board = new Board(4, 1);
            board.TryAddArrow(1, 0, 0, Direction.Right, out _);
            board.TryAddArrow(3, 3, 0, Direction.Right, out _);
            board.TryAddArrow(2, 1, 0, Direction.Right, out _);
            return board;
        }

        /// <summary>A free arrow leaves the same live board immediately without a mistake.</summary>
        [Test]
        public void FreeArrowExitsImmediately()
        {
            Board board = Chain();
            var session = new GameSession(board);
            Arrow arrow = board.GetArrowById(3);
            MoveResult result = session.TryMove(arrow);
            Assert.That(result.Outcome, Is.EqualTo(MoveOutcome.Exited));
            Assert.That(result.Arrow, Is.SameAs(arrow));
            Assert.That(result.Blocker, Is.Null);
            Assert.That(result.BoardCleared, Is.False);
            Assert.That(result.MistakeCount, Is.Zero);
            Assert.That(board.GetArrowById(3), Is.Null);
            Assert.That(session.Board, Is.SameAs(board));
        }

        /// <summary>A blocked move reports the nearest blocker without changing any arrows.</summary>
        [Test]
        public void BlockedArrowCountsMistakeAndPreservesBoard()
        {
            Board board = Chain();
            var session = new GameSession(board);
            Arrow first = board.GetArrowById(1);
            Arrow nearest = board.GetArrowById(2);
            MoveResult result = session.TryMove(first);
            Assert.That(result.Outcome, Is.EqualTo(MoveOutcome.Blocked));
            Assert.That(result.Blocker, Is.SameAs(nearest));
            Assert.That(result.BoardCleared, Is.False);
            Assert.That(result.MistakeCount, Is.EqualTo(1));
            Assert.That(board.Arrows.Count, Is.EqualTo(3));
            Assert.That(board.GetArrowAt(0, 0), Is.SameAs(first));
            Assert.That(board.GetArrowAt(1, 0), Is.SameAs(nearest));
            Assert.That(board.GetArrowAt(3, 0).Id, Is.EqualTo(3));
        }

        /// <summary>Mistakes accumulate without removing arrows or failing the level.</summary>
        [Test]
        public void RepeatedBlockedTapsCountIndividually()
        {
            var session = new GameSession(Chain());
            Arrow arrow = session.Board.GetArrowById(1);
            for (int i = 1; i <= 3; i++) Assert.That(session.TryMove(arrow).MistakeCount, Is.EqualTo(i));
            Assert.That(session.MistakeCount, Is.EqualTo(3));
            Assert.That(session.Board.Arrows.Count, Is.EqualTo(3));
            Assert.That(session.IsCleared, Is.False);
        }

        /// <summary>Null, foreign, and previously removed arrows cannot mutate the session.</summary>
        [Test]
        public void InvalidArrowsAreIgnored()
        {
            var session = new GameSession(Chain());
            Arrow removed = session.Board.GetArrowById(3);
            session.TryMove(removed);
            var foreign = new Arrow(1, 0, 0, Direction.Right);
            foreach (Arrow arrow in new[] { null, removed, foreign })
            {
                MoveResult result = session.TryMove(arrow);
                Assert.That(result.Outcome, Is.EqualTo(MoveOutcome.Ignored));
                Assert.That(result.Arrow, Is.SameAs(arrow));
                Assert.That(result.Blocker, Is.Null);
                Assert.That(result.BoardCleared, Is.False);
                Assert.That(result.MistakeCount, Is.Zero);
            }
            Assert.That(session.Board.Arrows.Count, Is.EqualTo(2));
        }

        /// <summary>Only the move that removes the last arrow reports cleared.</summary>
        [Test]
        public void LastArrowClearsOnlyOnce()
        {
            var board = new Board(1, 1);
            board.TryAddArrow(1, 0, 0, Direction.Up, out Arrow arrow);
            var session = new GameSession(board);
            Assert.That(session.TryMove(arrow).BoardCleared, Is.True);
            Assert.That(session.IsCleared, Is.True);
            MoveResult again = session.TryMove(arrow);
            Assert.That(again.Outcome, Is.EqualTo(MoveOutcome.Ignored));
            Assert.That(again.BoardCleared, Is.False);
            Assert.That(again.MistakeCount, Is.Zero);
        }

        /// <summary>Wrong-order taps count two mistakes; reversing the chain then clears it.</summary>
        [Test]
        public void WrongOrderThenChainClearWorks()
        {
            var session = new GameSession(Chain());
            Arrow a = session.Board.GetArrowById(1);
            Arrow b = session.Board.GetArrowById(2);
            Arrow c = session.Board.GetArrowById(3);
            Assert.That(session.TryMove(a).Outcome, Is.EqualTo(MoveOutcome.Blocked));
            Assert.That(session.TryMove(b).Outcome, Is.EqualTo(MoveOutcome.Blocked));
            Assert.That(session.TryMove(c).BoardCleared, Is.False);
            Assert.That(session.TryMove(b).BoardCleared, Is.False);
            MoveResult final = session.TryMove(a);
            Assert.That(final.BoardCleared, Is.True);
            Assert.That(final.MistakeCount, Is.EqualTo(2));
        }

        /// <summary>Reset uses the supplied instance and discards prior mistakes.</summary>
        [Test]
        public void ResetReplacesBoardAndMistakes()
        {
            var session = new GameSession(Chain());
            session.TryMove(session.Board.GetArrowById(1));
            var replacement = new Board(2, 2);
            replacement.TryAddArrow(8, 1, 1, Direction.Up, out _);
            session.Reset(replacement);
            Assert.That(session.Board, Is.SameAs(replacement));
            Assert.That(session.MistakeCount, Is.Zero);
            Assert.That(session.IsCleared, Is.False);
        }

        /// <summary>A session cannot be created with a missing board.</summary>
        [Test]
        public void NullConstructorThrows() => Assert.Throws<ArgumentNullException>(() => new GameSession(null));

        /// <summary>An invalid reset preserves the existing state.</summary>
        [Test]
        public void NullResetThrowsWithoutChangingSession()
        {
            Board board = Chain();
            var session = new GameSession(board);
            session.TryMove(board.GetArrowById(1));
            Assert.Throws<ArgumentNullException>(() => session.Reset(null));
            Assert.That(session.Board, Is.SameAs(board));
            Assert.That(session.MistakeCount, Is.EqualTo(1));
        }

        /// <summary>Every sample clears through session moves with zero mistakes.</summary>
        [TestCase("TestLevels/legacy_level_001")]
        [TestCase("TestLevels/legacy_level_002")]
        [TestCase("TestLevels/legacy_level_003")]
        public void SamplesClearWithOnlyFreeMoves(string path)
        {
            LevelLoadResult loaded = LevelRepository.LoadFromResources(path);
            Assert.That(loaded.Success, Is.True);
            var session = new GameSession(loaded.Board);
            while (!session.IsCleared)
            {
                Arrow free = null;
                foreach (Arrow arrow in session.Board.Arrows)
                    if (session.Board.CanExit(arrow)) { free = arrow; break; }
                Assert.That(free, Is.Not.Null, path);
                Assert.That(session.TryMove(free).Outcome, Is.EqualTo(MoveOutcome.Exited));
            }
            Assert.That(session.MistakeCount, Is.Zero);
        }

        /// <summary>Facing arrows remain blocked indefinitely without any loss condition.</summary>
        [Test]
        public void FacingArrowsCountMistakesWithoutChangingBoard()
        {
            var board = new Board(2, 1);
            board.TryAddArrow(1, 0, 0, Direction.Right, out Arrow a);
            board.TryAddArrow(2, 1, 0, Direction.Left, out Arrow b);
            var session = new GameSession(board);
            Assert.That(session.TryMove(a).Blocker, Is.SameAs(b));
            Assert.That(session.TryMove(b).Blocker, Is.SameAs(a));
            Assert.That(session.MistakeCount, Is.EqualTo(2));
            Assert.That(board.Arrows.Count, Is.EqualTo(2));
        }
    }
}
