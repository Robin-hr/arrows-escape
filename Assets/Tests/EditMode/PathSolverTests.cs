using System.Collections.Generic;
using System.Linq;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Independent full-cell search cross-checks compact solvers.</summary>
    public class PathSolverTests
    {
        private static bool Solve(Board b,HashSet<string> dead)
        {
            if(b.IsCleared())return true;string key=PathTestBoards.Signature(b);if(dead.Contains(key))return false;
            foreach(Arrow a in b.Arrows)
            {
                int gap=0;bool blocked=false;int dx=a.Direction.GetDx(),dy=a.Direction.GetDy();
                for(int x=a.X+dx,y=a.Y+dy;b.InBounds(x,y);x+=dx,y+=dy)
                {if(b.Arrows.Any(other=>other.Cells.Any(c=>c.X==x&&c.Y==y))){blocked=true;break;}gap++;}
                if(blocked&&(a.Kind!=ArrowKind.Ice||gap==0))continue;
                Board copy=b.Clone();Arrow current=copy.GetArrowById(a.Id);
                if(!blocked)copy.Remove(current);else copy.TryMoveArrow(current,a.X+dx*gap,a.Y+dy*gap,out _);
                if(Solve(copy,dead))return true;
            }
            dead.Add(key);return false;
        }
        [Test] public void ThreeHundredRandomBoardsAgree()
        {
            int solved=0,unsolved=0;
            for(int seed=0;seed<300;seed++)
            {
                var r=new SeededRandom(seed);var b=new Board(5,4);int ice=r.NextInt(3);
                for(int id=0;id<7;id++)
                {
                    var cells=new List<Cell>{new Cell(r.NextInt(5),r.NextInt(4))};int length=id<ice?1:r.NextInt(1,6);
                    for(int step=1;step<length;step++)
                    {Cell c=cells[cells.Count-1];Direction d=(Direction)r.NextInt(4);var next=new Cell(c.X+d.GetDx(),c.Y+d.GetDy());if(!b.InBounds(next.X,next.Y)||cells.Contains(next))break;cells.Add(next);}
                    Direction dir=(Direction)r.NextInt(4);
                    if(cells.Count>1){Cell a=cells[cells.Count-2],z=cells[cells.Count-1];dir=z.X>a.X?Direction.Right:z.X<a.X?Direction.Left:z.Y>a.Y?Direction.Up:Direction.Down;}
                    b.TryAddArrow(new Arrow(id,cells,dir,id<ice?ArrowKind.Ice:ArrowKind.Normal),out _);
                }
                bool expected=Solve(b,new HashSet<string>());if(expected)solved++;else unsolved++;
                var actual=PuzzleSolver.Analyze(b);Assert.That(actual.Status==SolveStatus.Solved,Is.EqualTo(expected),"seed "+seed);
                Assert.That(LevelSolvability.IsSolvable(b),Is.EqualTo(expected));
                var analysis=LevelAnalyzer.Analyze(b,new AnalysisOptions(5000,8));if(analysis.ExhaustiveComplete)Assert.That(analysis.SolutionCount>0,Is.EqualTo(expected));
            }
            TestContext.WriteLine($"Cross-check solved={solved}, unsolvable={unsolved}");Assert.That(solved,Is.GreaterThanOrEqualTo(30));Assert.That(unsolved,Is.GreaterThanOrEqualTo(30));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)] public void FixturesSolveAndHintsClear(int n)
        {
            Board b=PathTestBoards.Load(n);string before=PathTestBoards.Signature(b);var result=PuzzleSolver.Analyze(b);Assert.That(result.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(PathTestBoards.Signature(b),Is.EqualTo(before));
            TestContext.WriteLine("PATH "+n+" SOLUTION "+string.Join(",",result.Path.Select(m=>m.ArrowId)));
            var hints=new List<int>();while(!b.IsCleared()){var h=HintFinder.Find(b);Assert.That(h.HasHint,Is.True);Assert.That(b.CanExit(h.Arrow),Is.True);hints.Add(h.Arrow.Id);b.Remove(h.Arrow);}TestContext.WriteLine("HINTS "+string.Join(",",hints));
        }
    }
}
