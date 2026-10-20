using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks mixed LIFO history and exact slide restoration without changing exit progress.</summary>
    public class GameSessionIceUndoTests
    {
        /// <summary>Slide undo preserves list index and MoveCount; mixed actions restore in LIFO order.</summary>
        [Test]public void MixedHistory(){var b=IceTestBoards.Cycle();string original=IceTestBoards.Key(b);var s=new GameSession(b);s.TryMove(b.GetArrowById(1));Assert.That(s.CanUndo,Is.True);s.TryMove(b.GetArrowById(4));Assert.That(s.MoveCount,Is.EqualTo(1));Assert.That(s.TryUndo().MoveKind,Is.EqualTo(UndoMoveKind.Exit));Assert.That(s.MoveCount,Is.Zero);var undo=s.TryUndo();Assert.That(undo.MoveKind,Is.EqualTo(UndoMoveKind.Slide));Assert.That(undo.Restored.X,Is.Zero);Assert.That(undo.Restored.Kind,Is.EqualTo(ArrowKind.Ice));Assert.That(b.Arrows[0].Id,Is.EqualTo(1));Assert.That(s.MoveCount,Is.Zero);Assert.That(s.UndoCount,Is.EqualTo(2));Assert.That(IceTestBoards.Key(b),Is.EqualTo(original));}
        /// <summary>Ice exits restore their kind, and failed slide undo preserves history.</summary>
        [Test]public void FailureAndIceExit(){var b=IceTestBoards.Cycle();var s=new GameSession(b);s.TryMove(b.GetArrowById(1));b.TryAddArrow(99,0,0,Direction.Up,out var conflict);string before=IceTestBoards.Key(b);Assert.That(s.TryUndo().Outcome,Is.EqualTo(UndoOutcome.Failed));Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));Assert.That(s.MoveCount+s.UndoCount,Is.Zero);b.Remove(conflict);Assert.That(s.TryUndo().Outcome,Is.EqualTo(UndoOutcome.Undone));var free=new Board(3,1);free.TryAddArrow(1,0,0,Direction.Up,ArrowKind.Ice,out var ice);free.TryAddArrow(2,2,0,Direction.Up,out _);s.Reset(free);s.TryMove(ice);Assert.That(s.TryUndo().Restored.Kind,Is.EqualTo(ArrowKind.Ice));}
        /// <summary>Mistakes survive undo and final completion still disables undo.</summary>
        [Test]public void MistakesAndClear(){var b=IceTestBoards.Cycle();var s=new GameSession(b);s.TryMove(b.GetArrowById(2));s.TryMove(b.GetArrowById(1));s.TryUndo();Assert.That(s.MistakeCount,Is.EqualTo(1));foreach(int id in new[]{1,4,3,2,1})s.TryMove(b.GetArrowById(id));Assert.That(s.TryUndo().Outcome,Is.EqualTo(UndoOutcome.LevelCleared));}
        /// <summary>Two hundred seeded legal sequences per ground-truth board undo to the original set.</summary>
        [TestCase(false)][TestCase(true)]public void RandomRoundTrips(bool trap)
        {
            for(int seed=1;seed<=200;seed++){var b=trap?IceTestBoards.Trap():IceTestBoards.Cycle();string original=IceTestBoards.Key(b);var s=new GameSession(b);var random=new DeterministicRandom(seed);
                for(int step=0;step<30&&b.Arrows.Count>1;step++){int start=(int)(random.NextFloat()*b.Arrows.Count);Arrow selected=null;for(int i=0;i<b.Arrows.Count;i++){var a=b.Arrows[(start+i)%b.Arrows.Count];if(b.CanExit(a)||b.CanSlide(a)){selected=a;break;}}if(selected==null)break;s.TryMove(selected);}
                while(s.CanUndo)Assert.That(s.TryUndo().Outcome,Is.EqualTo(UndoOutcome.Undone));Assert.That(IceTestBoards.Key(b),Is.EqualTo(original));Assert.That(s.MoveCount,Is.Zero);
            }
        }
    }
}
