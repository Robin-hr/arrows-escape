using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Ice hints always come from a complete safe solver path.</summary>
    public class HintFinderIceTests
    {
        /// <summary>Cycle requires a slide hint and asking never mutates the board.</summary>
        [Test]public void Cycle(){var b=IceTestBoards.Cycle();string before=IceTestBoards.Key(b);var hint=HintFinder.Find(b);Assert.That(hint.Arrow.Id,Is.EqualTo(1));Assert.That(hint.IsSlide,Is.True);Assert.That(hint.NewlyFreed+hint.Dependents,Is.Zero);Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));}
        /// <summary>Following hints clears both ground-truth boards without mistakes.</summary>
        [TestCase(false)][TestCase(true)]public void Follow(bool trap){var b=trap?IceTestBoards.Trap():IceTestBoards.Cycle();var s=new GameSession(b);while(!s.IsCleared){var h=HintFinder.Find(b);Assert.That(h.HasHint,Is.True);var r=s.TryMove(h.Arrow);Assert.That(r.Outcome,Is.EqualTo(h.IsSlide?MoveOutcome.Slid:MoveOutcome.Exited));}Assert.That(s.MistakeCount,Is.Zero);}
        /// <summary>One hundred solvable seeded ice states remain solvable after every suggested move.</summary>
        [Test]public void SafeHintsProperty()
        {int tested=0;for(int seed=0;seed<3000&&tested<100;seed++){var b=IceTestBoards.RandomBoard(seed);if(!b.HasIceArrows||PuzzleSolver.Analyze(b).Status!=SolveStatus.Solved)continue;tested++;var s=new GameSession(b);while(!s.IsCleared){var h=HintFinder.Find(b);Assert.That(h.HasHint,Is.True,"seed "+seed);s.TryMove(h.Arrow);Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Solved));}}Assert.That(tested,Is.EqualTo(100));}
        /// <summary>Dead states have no hint; normal levels still have their original free-arrow hints.</summary>
        [Test]public void DeadAndNormal(){var b=IceTestBoards.Trap();var s=new GameSession(b);s.TryMove(b.GetArrowById(2));s.TryMove(b.GetArrowById(1));Assert.That(HintFinder.Find(b).HasHint,Is.False);var normal=LevelRepository.LoadFromResources("Levels/level_001").Board;var h=HintFinder.Find(normal);Assert.That(h.Arrow.Id,Is.EqualTo(4));Assert.That(h.IsSlide,Is.False);Assert.That(h.NewlyFreed,Is.EqualTo(1));}
    }
}
