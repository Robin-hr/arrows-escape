using System.Linq;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Path history preserves all cells and the existing append-on-undo rule.</summary>
    public class PathSessionTests
    {
        [Test] public void ExitUndoAndMistake()
        {
            Board b=PathTestBoards.Load();var s=new GameSession(b);Assert.That(s.TryMove(b.GetArrowById(1)).Outcome,Is.EqualTo(MoveOutcome.Blocked));
            Arrow a=b.GetArrowById(2);s.TryMove(a);Assert.That(s.MoveCount,Is.EqualTo(1));var r=s.TryUndo();Assert.That(r.Restored.Cells,Is.EqualTo(a.Cells));Assert.That(b.Arrows.Last(),Is.SameAs(r.Restored));Assert.That(s.MistakeCount,Is.EqualTo(1));Assert.That(s.MoveCount,Is.Zero);
        }
        [Test] public void RandomSequencesUndoById()
        {
            for(int seed=0;seed<200;seed++)
            {
                Board b=PathTestBoards.Load(2);string before=PathTestBoards.Signature(b);var s=new GameSession(b);var rng=new SeededRandom(seed);
                for(int step=0;step<40;step++)
                {if(rng.NextInt(3)==0&&s.CanUndo)s.TryUndo();else if(b.Arrows.Count>1){var free=b.Arrows.Where(b.CanExit).ToArray();s.TryMove(free[rng.NextInt(free.Length)]);}}
                while(s.CanUndo)s.TryUndo();Assert.That(PathTestBoards.Signature(b),Is.EqualTo(before));Assert.That(s.MoveCount,Is.Zero);
            }
        }
    }
}
