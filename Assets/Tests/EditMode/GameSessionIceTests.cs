using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks exact cycle rules and that slides never advance exit progress.</summary>
    public class GameSessionIceTests
    {
        /// <summary>The first ice move replaces its instance, then its adjacent blocker counts as a mistake.</summary>
        [Test]public void SlideAndBlocked(){var b=IceTestBoards.Cycle();var s=new GameSession(b);Arrow old=b.GetArrowById(1);var r=s.TryMove(old);Assert.That(r.Outcome,Is.EqualTo(MoveOutcome.Slid));Assert.That(r.MovedArrow,Is.SameAs(b.GetArrowById(1)));Assert.That(r.MovedArrow.X,Is.EqualTo(2));Assert.That(r.SlideCells,Is.EqualTo(2));Assert.That(r.Blocker,Is.Null);Assert.That(s.MoveCount+s.MistakeCount,Is.Zero);Assert.That(b.Arrows.Count,Is.EqualTo(4));Assert.That(s.TryMove(old).Outcome,Is.EqualTo(MoveOutcome.Ignored));Assert.That(s.TryMove(r.MovedArrow).Outcome,Is.EqualTo(MoveOutcome.Blocked));Assert.That(s.MistakeCount,Is.EqualTo(1));}
        /// <summary>Every normal arrow in the initial cycle is blocked, never slid.</summary>
        [TestCase(2)][TestCase(3)][TestCase(4)]public void NormalBlocked(int id){var b=IceTestBoards.Cycle();var s=new GameSession(b);string before=IceTestBoards.Key(b);Assert.That(s.TryMove(b.GetArrowById(id)).Outcome,Is.EqualTo(MoveOutcome.Blocked));Assert.That(s.MistakeCount,Is.EqualTo(1));Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));}
        /// <summary>The required five-tap solution ends at four moves, with clear only on the last exit.</summary>
        [Test]public void FullCycle(){var b=IceTestBoards.Cycle();var s=new GameSession(b);int[] ids={1,4,3,2,1};for(int i=0;i<ids.Length;i++){var r=s.TryMove(b.GetArrowById(ids[i]));Assert.That(r.BoardCleared,Is.EqualTo(i==4));Assert.That(r.Outcome,Is.EqualTo(i==0?MoveOutcome.Slid:MoveOutcome.Exited));}Assert.That(s.MoveCount,Is.EqualTo(4));Assert.That(s.MistakeCount,Is.Zero);Assert.That(s.TryMove(null).Outcome,Is.EqualTo(MoveOutcome.Ignored));s.Reset(IceTestBoards.Cycle());Assert.That(s.MoveCount+s.UndoCount+s.MistakeCount,Is.Zero);Assert.That(s.CanUndo,Is.False);}
    }
}
