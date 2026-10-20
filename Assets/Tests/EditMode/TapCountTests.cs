using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14A regression coverage with isolated state and no player-save access.</summary>
    public sealed class TapCountTests
    {
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void AcceptedMovesOnlyAndUndoNeverSubtracts()
        {
            var session=new GameSession(IceTestBoards.Cycle());
            Assert.That(session.TryMove(session.Board.GetArrowById(2)).Outcome,Is.EqualTo(MoveOutcome.Blocked));
            Assert.That(session.TapCount,Is.EqualTo(1));
            Arrow old=session.Board.GetArrowById(1);
            Assert.That(session.TryMove(old).Outcome,Is.EqualTo(MoveOutcome.Slid));
            Assert.That(session.TapCount,Is.EqualTo(2));
            Assert.That(session.TryMove(old).Outcome,Is.EqualTo(MoveOutcome.Ignored));
            session.TryMove(null);Assert.That(session.TapCount,Is.EqualTo(2));
            session.TryUndo();Assert.That(session.TapCount,Is.EqualTo(2));Assert.That(session.MoveCount,Is.Zero);
            session.Reset(new Board(2,1));session.Board.TryAddArrow(1,0,0,Direction.Left,out var a);session.Board.TryAddArrow(2,1,0,Direction.Right,out _);
            session.TryMove(a);Assert.That(session.TapCount,Is.EqualTo(1));session.TryUndo();Assert.That(session.TapCount,Is.EqualTo(1));
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void ControllerHintPauseAndRestart()
        {
            using(var scene=new ProgressTestScene())
            {
                scene.Bootstrap.ShowLevel(0);int par=scene.Game.Par;
                scene.Game.ShowHint();Assert.That(scene.Game.TapCount,Is.Zero);
                int events=0;scene.Game.TapCountChanged+=_=>events++;
                scene.Tap(0,0);Assert.That(scene.Game.TapCount,Is.EqualTo(1));Assert.That(events,Is.EqualTo(1));
                scene.Game.SetPaused(true);scene.Tap(0,0);Assert.That(scene.Game.TapCount,Is.EqualTo(1));
                scene.Game.RestartLevel();Assert.That(scene.Game.TapCount,Is.Zero);Assert.That(scene.Game.Par,Is.EqualTo(par));
            }
        }
    }
}
