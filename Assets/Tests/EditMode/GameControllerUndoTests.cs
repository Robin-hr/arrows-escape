using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Drives undo through the same tap and animation path used in play.</summary>
    public class GameControllerUndoTests
    {
        private GameObject host; private GameController game; private BoardView view; private ArrowAnimator animator; private Board board;
        /// <summary>Creates a three-arrow chain.</summary>
        [SetUp] public void SetUp() { host = new GameObject(); game = host.AddComponent<GameController>(); view = host.GetComponent<BoardView>(); animator = host.GetComponent<ArrowAnimator>(); board = new Board(3, 1); for (int i = 0; i < 3; i++) board.TryAddArrow(i, i, 0, Direction.Right, out _); game.StartLevel(board); }
        /// <summary>Removes runtime objects.</summary>
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        private void Tap(int x) { var taps = host.GetComponent<TapController>(); Vector2 p = view.CellToWorld(x, 0); taps.PointerDown(1, p); taps.PointerUp(1, p); }
        /// <summary>Undo before or after a slide leaves exactly one clean replacement.</summary>
        [TestCase(0f)][TestCase(0.1f)][TestCase(1f)] public void UndoSlide(float time)
        {
            view.TryGetArrowView(2, out ArrowView old); Tap(2); animator.Tick(time); game.Undo();
            Assert.That(old == null, Is.True); Assert.That(board.GetArrowById(2), Is.Not.Null); Assert.That(game.MoveCount, Is.Zero);
            Assert.That(view.TryGetArrowView(2, out ArrowView fresh), Is.True); Assert.That(fresh.transform.position, Is.EqualTo(view.CellToWorld(2, 0))); Assert.That(fresh.Alpha, Is.EqualTo(1));
            Assert.DoesNotThrow(() => animator.Tick(10)); Assert.That(host.GetComponentsInChildren<ArrowView>().Length, Is.EqualTo(3));
        }
        /// <summary>Exit and undo each announce progress and undo availability.</summary>
        [Test] public void Events() { int moves = 0, undo = 0, restored = -1; game.MoveCountChanged += () => moves++; game.UndoStateChanged += () => undo++; game.ArrowRestored += a => restored = a.Id; Tap(2); game.Undo(); Assert.That(moves, Is.EqualTo(2)); Assert.That(undo, Is.EqualTo(2)); Assert.That(restored, Is.EqualTo(2)); }
        /// <summary>Unavailable undo is silent.</summary>
        [Test] public void Nothing() { int events = 0; game.MoveCountChanged += () => events++; game.UndoStateChanged += () => events++; game.ArrowRestored += a => events++; game.Undo(); Assert.That(events, Is.Zero); }
        /// <summary>Mistakes survive undo.</summary>
        [Test] public void Mistakes() { Tap(0); Tap(2); game.Undo(); Assert.That(game.MistakeCount, Is.EqualTo(1)); Assert.That(game.UndoCount, Is.EqualTo(1)); }
        /// <summary>Cleared attempts cannot be undone.</summary>
        [Test] public void Final() { Tap(2); Tap(1); LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0"); Tap(0); game.Undo(); Assert.That(game.IsCleared, Is.True); Assert.That(game.MoveCount, Is.EqualTo(3)); Assert.That(game.UndoCount, Is.Zero); }
        /// <summary>Name and shown board are available when LevelStarted fires.</summary>
        [Test] public void NamedStart() { int events = 0; game.LevelStarted += () => { events++; Assert.That(game.LevelName, Is.EqualTo("Named")); Assert.That(view.TryGetBoard(out Board shown), Is.True); Assert.That(shown, Is.SameAs(board)); }; game.StartLevel(board, "Named"); Assert.That(events, Is.EqualTo(1)); }
        /// <summary>Restored objects can be tapped again through the same shared board.</summary>
        [Test] public void ExitRestored() { Tap(2); game.Undo(); Tap(2); Assert.That(game.MoveCount, Is.EqualTo(1)); Assert.That(board.GetArrowById(2), Is.Null); view.TryGetBoard(out Board shown); Assert.That(shown, Is.SameAs(game.Session.Board)); }
        /// <summary>Arbitrary boards can restart from a private initial snapshot.</summary>
        [Test] public void RestartCustom() { Tap(0); Tap(2); game.RestartLevel(); Assert.That(game.MoveCount, Is.Zero); Assert.That(game.MistakeCount, Is.Zero); Assert.That(game.StartingArrowCount, Is.EqualTo(3)); Assert.That(game.Session.Board.Arrows.Count, Is.EqualTo(3)); }
    }
}
