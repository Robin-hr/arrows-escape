using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Tests gameplay pause flags without relying on real frames.</summary>
    public class GameControllerPauseTests
    {
        private ProgressTestScene s;
        /// <summary>Builds the first sample.</summary>
        [SetUp] public void Setup() { s = new ProgressTestScene(); s.Bootstrap.ShowLevel(0); }
        /// <summary>Removes runtime objects.</summary>
        [TearDown] public void Cleanup() => s.Dispose();
        /// <summary>Free and blocked taps have no effect while paused.</summary>
        [Test] public void NoTaps() { int exits = 0; s.Game.ArrowExited += a => exits++; s.Game.SetPaused(true); s.Tap(3, 2); s.Tap(0, 0); Assert.That(s.Game.MoveCount, Is.Zero); Assert.That(s.Game.MistakeCount, Is.Zero); Assert.That(exits, Is.Zero); s.Game.SetPaused(false); s.Tap(3, 2); Assert.That(s.Game.MoveCount, Is.EqualTo(1)); }
        /// <summary>Undo is suppressed until resumed.</summary>
        [Test] public void NoUndo() { s.Tap(3, 2); s.Game.SetPaused(true); s.Game.Undo(); Assert.That(s.Game.MoveCount, Is.EqualTo(1)); s.Game.SetPaused(false); s.Game.Undo(); Assert.That(s.Game.MoveCount, Is.Zero); }
        /// <summary>Restart and replacement levels unpause gameplay.</summary>
        [Test] public void Restart() { s.Game.SetPaused(true); s.Game.RestartLevel(); Assert.That(s.Game.IsPaused, Is.False); s.Game.SetPaused(true); s.Game.StartLevel(s.Game.Session.Board.Clone()); Assert.That(s.Game.IsPaused, Is.False); }
        /// <summary>Pausing cancels an active highlighted press.</summary>
        [Test] public void PressCancelled() { var view = s.Game.GetComponent<BoardView>(); var taps = s.Game.GetComponent<TapController>(); Vector2 point = view.CellToWorld(3, 2); taps.PointerDown(1, point); view.TryGetArrowView(4, out ArrowView arrow); Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor)); s.Game.SetPaused(true); Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor)); s.Game.SetPaused(false); taps.PointerUp(1, point); Assert.That(s.Game.MoveCount, Is.Zero); }
        /// <summary>Automatic-frame pause skips Tick, while explicit Tick remains testable.</summary>
        [Test] public void AnimatorPause() { var view = s.Game.GetComponent<BoardView>(); var animator = s.Game.GetComponent<ArrowAnimator>(); view.TryGetArrowView(4, out ArrowView arrow); animator.PlaySlideOut(arrow, Direction.Up, 4, 4, 3, 2); animator.SetPaused(true); animator.AdvanceFrame(0.1f); Assert.That(arrow.Offset, Is.EqualTo(Vector2.zero)); animator.Tick(0.1f); Assert.That(arrow.Offset.y, Is.GreaterThan(0)); animator.SetPaused(false); animator.AdvanceFrame(1); Assert.That(arrow == null, Is.True); }
    }
}
