using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks exit-only LIFO history and persistent mistakes.</summary>
    public class GameSessionUndoTests
    {
        private Board board;
        private GameSession session;
        /// <summary>Creates four independent outward arrows, retaining one to avoid clearing.</summary>
        [SetUp] public void SetUp()
        {
            board = new Board(4, 1);
            for (int i = 0; i < 4; i++) board.TryAddArrow(i, i, 0, Direction.Up, out _);
            session = new GameSession(board);
        }
        /// <summary>A fresh attempt has no undo history.</summary>
        [Test] public void Fresh()
        { Assert.That(session.CanUndo, Is.False); Assert.That(session.StartingArrowCount, Is.EqualTo(4)); Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.NothingToUndo)); }
        /// <summary>Restoration uses a new instance and appends it to insertion order.</summary>
        [Test] public void ExitUndo()
        {
            Arrow old = board.GetArrowById(0); session.TryMove(old);
            Assert.That(session.MoveCount, Is.EqualTo(1)); Assert.That(session.CanUndo, Is.True);
            UndoResult result = session.TryUndo();
            Assert.That(result.Outcome, Is.EqualTo(UndoOutcome.Undone)); Assert.That(result.Restored, Is.Not.SameAs(old));
            Assert.That(result.Restored.Id, Is.EqualTo(0)); Assert.That(result.Restored.X, Is.EqualTo(0)); Assert.That(result.Restored.Y, Is.EqualTo(0)); Assert.That(result.Restored.Direction, Is.EqualTo(Direction.Up));
            Assert.That(board.Arrows[3], Is.SameAs(result.Restored)); Assert.That(result.MoveCount, Is.Zero); Assert.That(session.UndoCount, Is.EqualTo(1));
        }
        /// <summary>Only exits enter history and mistakes survive a later undo.</summary>
        [Test] public void BlockedHasNoHistory()
        {
            board = new Board(3, 1); board.TryAddArrow(1, 0, 0, Direction.Right, out Arrow a); board.TryAddArrow(2, 1, 0, Direction.Up, out Arrow b); session.Reset(board);
            session.TryMove(a); Assert.That(session.CanUndo, Is.False); session.TryMove(b); session.TryUndo(); Assert.That(session.MistakeCount, Is.EqualTo(1));
        }
        /// <summary>Multiple exits restore in reverse order and preserve the arrow set.</summary>
        [Test] public void ReverseOrder()
        {
            Board original = board.Clone();
            for (int i = 0; i < 3; i++) session.TryMove(board.GetArrowById(i));
            for (int i = 2; i >= 0; i--) Assert.That(session.TryUndo().Restored.Id, Is.EqualTo(i));
            AssertSameSet(original, board);
        }
        /// <summary>A restored arrow can exit and enter history again.</summary>
        [Test] public void ExitAgain()
        { session.TryMove(board.GetArrowById(0)); Arrow arrow = session.TryUndo().Restored; Assert.That(session.TryMove(arrow).Outcome, Is.EqualTo(MoveOutcome.Exited)); Assert.That(session.MoveCount, Is.EqualTo(1)); }
        /// <summary>Clearing freezes history at full progress.</summary>
        [Test] public void ClearedIsFinal()
        {
            MoveResult last = null; while (!board.IsCleared()) last = session.TryMove(board.Arrows[0]);
            Assert.That(last.BoardCleared, Is.True); Assert.That(session.MoveCount, Is.EqualTo(session.StartingArrowCount));
            Assert.That(session.CanUndo, Is.False); Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.LevelCleared)); Assert.That(session.UndoCount, Is.Zero); Assert.That(board.IsCleared(), Is.True);
        }
        /// <summary>Restoring a blocker blocks the original path again.</summary>
        [Test] public void BlockerReturns()
        {
            var b = new Board(3, 2); b.TryAddArrow(1, 0, 0, Direction.Right, out Arrow a); b.TryAddArrow(2, 1, 0, Direction.Up, out Arrow blocker); session.Reset(b);
            session.TryMove(blocker); Assert.That(b.CanExit(a), Is.True); session.TryUndo(); Assert.That(b.CanExit(a), Is.False);
        }
        /// <summary>Reset clears every attempt counter and history.</summary>
        [Test] public void Reset()
        { session.TryMove(board.GetArrowById(0)); session.TryUndo(); session.TryMove(board.GetArrowById(1)); session.Reset(new Board(2, 2)); Assert.That(session.MoveCount, Is.Zero); Assert.That(session.UndoCount, Is.Zero); Assert.That(session.MistakeCount, Is.Zero); Assert.That(session.StartingArrowCount, Is.Zero); }
        /// <summary>A conflicting external mutation must not consume the pending record.</summary>
        [Test] public void FailedRestorePreservesHistory()
        {
            session.TryMove(board.GetArrowById(0)); board.TryAddArrow(99, 0, 0, Direction.Up, out Arrow conflict);
            Assert.DoesNotThrow(() => Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.Failed)));
            Assert.That(session.MoveCount, Is.EqualTo(1)); Assert.That(session.UndoCount, Is.Zero); Assert.That(board.GetArrowById(99), Is.SameAs(conflict));
            board.Remove(conflict); Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.Undone));
        }
        /// <summary>All sample boards survive partial play and unlimited undo.</summary>
        [TestCase("TestLevels/legacy_level_001")][TestCase("TestLevels/legacy_level_002")][TestCase("TestLevels/legacy_level_003")]
        public void Samples(string path)
        {
            board = LevelRepository.LoadFromResources(path).Board; Board original = board.Clone(); session.Reset(board);
            for (int i = 0; i < original.Arrows.Count - 1; i++)
            { Arrow free = null; foreach (Arrow a in board.Arrows) if (board.CanExit(a)) { free = a; break; } Assert.That(free, Is.Not.Null); session.TryMove(free); }
            while (session.CanUndo) Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.Undone));
            AssertSameSet(original, board); Assert.That(session.MoveCount, Is.Zero);
        }
        private static void AssertSameSet(Board expected, Board actual)
        {
            Assert.That(actual.Arrows.Count, Is.EqualTo(expected.Arrows.Count));
            foreach (Arrow a in expected.Arrows)
            { Arrow b = actual.GetArrowById(a.Id); Assert.That(b, Is.Not.Null); Assert.That(b.X, Is.EqualTo(a.X)); Assert.That(b.Y, Is.EqualTo(a.Y)); Assert.That(b.Direction, Is.EqualTo(a.Direction)); }
        }
    }
}
