using System.Linq;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies all sandbox constraints with the real search solver.</summary>
    public class SampleIceLevelsTests
    {
        /// <summary>Every test resource solves, and its solution and repeated hints replay legally.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)]public void Resources(int index)
        {var loaded=LevelRepository.LoadFromResources("TestLevels/ice_00"+index);Assert.That(loaded.Success,Is.True);var b=loaded.Board;var r=PuzzleSolver.Analyze(b);Assert.That(r.Status,Is.EqualTo(SolveStatus.Solved));TestContext.WriteLine($"ICE_LEVEL {index} nodes={r.NodesVisited} path="+string.Join(";",r.Path.Select(m=>$"{m.ArrowId}:{(m.IsSlide?$"slide({m.ToX},{m.ToY})":"exit")}")));if(index!=2)Assert.That(LevelSolvability.IsSolvable(IceTestBoards.NormalCopy(b)),Is.False);var s=new GameSession(b);while(!s.IsCleared){var h=HintFinder.Find(b);Assert.That(h.HasHint,Is.True);TestContext.WriteLine($"ICE_HINT {index} id={h.Arrow.Id} cell=({h.Arrow.X},{h.Arrow.Y}) direction={h.Arrow.Direction} slide={h.IsSlide}");s.TryMove(h.Arrow);}Assert.That(s.MistakeCount,Is.Zero);}
        /// <summary>The two sandbox traps have explicit legal dead-end witnesses within the required depth.</summary>
        [TestCase(2)][TestCase(3)]public void DeadEnd(int index)
        {var b=LevelRepository.LoadFromResources("TestLevels/ice_00"+index).Board;var s=new GameSession(b);if(index==2){s.TryMove(b.GetArrowById(2));Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Solved));s.TryMove(b.GetArrowById(1));}else{Assert.That(b.Width,Is.InRange(6,7));Assert.That(b.Height,Is.InRange(6,7));Assert.That(b.Arrows.Count,Is.InRange(12,18));Assert.That(b.Arrows.Count(a=>a.Kind==ArrowKind.Ice),Is.GreaterThanOrEqualTo(2));Assert.That(LevelSolvability.CountFreeArrows(b),Is.GreaterThanOrEqualTo(2));foreach(int id in new[]{13,6,5})Assert.That(s.TryMove(b.GetArrowById(id)).Outcome,Is.Not.EqualTo(MoveOutcome.Blocked));}Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Unsolvable));}
        /// <summary>Measures the bounded stuck check on every reachable state, including every trap.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)]public void ReachableStateCost(int index)
        {
            var pending=new Stack<Board>();var seen=new HashSet<string>();int maxNodes=0;
            pending.Push(LevelRepository.LoadFromResources("TestLevels/ice_00"+index).Board);
            while(pending.Count>0)
            {
                Board b=pending.Pop();if(!seen.Add(IceTestBoards.Key(b)))continue;
                if(b.HasIceArrows){var result=PuzzleSolver.Analyze(b,20000);Assert.That(result.NodesVisited,Is.LessThanOrEqualTo(20000));maxNodes=System.Math.Max(maxNodes,result.NodesVisited);}
                foreach(Arrow a in b.Arrows)
                {
                    bool exit=b.CanExit(a);int gap=b.CountFreeCellsAhead(a);
                    if(!exit&&(a.Kind!=ArrowKind.Ice||gap==0))continue;
                    var child=b.Clone();var move=new SolverMove(a.Id,!exit,a.X+a.Direction.GetDx()*gap,a.Y+a.Direction.GetDy()*gap);
                    Assert.That(IceTestBoards.Apply(child,move),Is.True);pending.Push(child);
                }
            }
            TestContext.WriteLine($"STUCK_COST level={index} reachable_states={seen.Count} largest_nodes={maxNodes}");
        }
        /// <summary>Sandbox resources never join the real level list.</summary>
        [Test]public void RealListUnchanged()=>OriginalCatalogAssertions.Check();
    }
}


