using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks bounded compact search against a separate unpruned Board-clone solver.</summary>
    public class PuzzleSolverTests
    {
        internal static bool BruteSolvable(Board board)=>Brute(board.Clone(),new HashSet<string>());
        private static bool Brute(Board board,HashSet<string> dead)
        {
            if(board.IsCleared())return true;string key=IceTestBoards.Key(board);if(dead.Contains(key))return false;
            foreach(Arrow a in board.Arrows)
            {
                bool exit=board.CanExit(a);int gap=board.CountFreeCellsAhead(a);
                if(!exit&&(a.Kind!=ArrowKind.Ice||gap<1))continue;
                Board child=board.Clone();Arrow current=child.GetArrowById(a.Id);
                if(exit)child.Remove(current);else child.TryMoveArrow(current,current.X+current.Direction.GetDx()*gap,current.Y+current.Direction.GetDy()*gap,out _);
                if(Brute(child,dead))return true;
            }
            dead.Add(key);return false;
        }
        /// <summary>The cycle path replays exactly and must begin with ice sliding.</summary>
        [Test]public void Cycle()
        {var b=IceTestBoards.Cycle();string before=IceTestBoards.Key(b);var r=PuzzleSolver.Analyze(b);Assert.That(r.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(r.Path[0],Is.EqualTo(new SolverMove(1,true,2,0)));Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));Assert.That(PuzzleSolver.Analyze(b).Path,Is.EqualTo(r.Path));var replay=b.Clone();foreach(var m in r.Path)Assert.That(IceTestBoards.Apply(replay,m),Is.True);Assert.That(replay.IsCleared(),Is.True);Assert.That(LevelSolvability.IsSolvable(IceTestBoards.NormalCopy(b)),Is.False);Assert.That(LevelSolvability.IsSolvable(b),Is.True);}
        /// <summary>The trap is recoverable after exit 2, but dead after slide 1.</summary>
        [Test]public void TrapAndSafety()
        {var b=IceTestBoards.Trap();Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Solved));Assert.That(PuzzleSolver.IsSafeExit(b,b.GetArrowById(3)),Is.True);Assert.That(PuzzleSolver.IsSafeExit(b,b.GetArrowById(2)),Is.False);Assert.That(PuzzleSolver.IsSafeExit(b,b.GetArrowById(5)),Is.False);var s=new GameSession(b);s.TryMove(b.GetArrowById(2));Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Solved));Assert.That(s.TryMove(b.GetArrowById(1)).Outcome,Is.EqualTo(MoveOutcome.Slid));Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Unsolvable));}
        /// <summary>Empty, invalid, budget-exhausted and opposing-arrow cases are explicit.</summary>
        [Test]public void Contracts()
        {Assert.Throws<ArgumentNullException>(()=>PuzzleSolver.Analyze(null));Assert.Throws<ArgumentOutOfRangeException>(()=>PuzzleSolver.Analyze(IceTestBoards.Cycle(),0));var empty=PuzzleSolver.Analyze(new Board(2,2));Assert.That(empty.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(empty.Path,Is.Empty);var unknown=PuzzleSolver.Analyze(IceTestBoards.Cycle(),1);Assert.That(unknown.Status,Is.EqualTo(SolveStatus.Unknown));Assert.That(unknown.Path,Is.Null);Assert.That(unknown.NodesVisited,Is.EqualTo(1));var b=new Board(2,1);b.TryAddArrow(1,0,0,Direction.Right,ArrowKind.Ice,out _);b.TryAddArrow(2,1,0,Direction.Left,out _);Assert.That(PuzzleSolver.Analyze(b).Status,Is.EqualTo(SolveStatus.Unsolvable));}
        /// <summary>Normal boards retain the existing greedy answer, including two hundred seeded boards.</summary>
        [Test]public void NormalAgreement()
        {foreach(string path in LevelRepository.GetLevelResourcePaths()){var b=LevelRepository.LoadFromResources(path).Board;Assert.That(PuzzleSolver.Analyze(b).Status==SolveStatus.Solved,Is.EqualTo(LevelSolvability.IsSolvable(b)));}for(int i=0;i<200;i++){var b=IceTestBoards.NormalCopy(IceTestBoards.RandomBoard(i));Assert.That(PuzzleSolver.Analyze(b).Status==SolveStatus.Solved,Is.EqualTo(LevelSolvability.IsSolvable(b)),"seed "+i);}}
        /// <summary>Three hundred meaningful seeded boards agree with independent exhaustive search.</summary>
        [Test]public void BruteForceCrossCheck()
        {
            int solved=0,unsolvable=0,slidesRequired=0;
            for(int seed=0;seed<300;seed++){
                Board b=IceTestBoards.RandomBoard(seed);bool brute=BruteSolvable(b);SolveResult actual=PuzzleSolver.Analyze(b);
                Assert.That(actual.Status,Is.Not.EqualTo(SolveStatus.Unknown),"seed "+seed);Assert.That(actual.Status==SolveStatus.Solved,Is.EqualTo(brute),"seed "+seed);
                if(brute){solved++;if(!BruteSolvable(IceTestBoards.NormalCopy(b)))slidesRequired++;var replay=b.Clone();foreach(var m in actual.Path)Assert.That(IceTestBoards.Apply(replay,m),Is.True);Assert.That(replay.IsCleared(),Is.True);}else unsolvable++;
            }
            TestContext.WriteLine($"CROSS_CHECK boards=300 solved={solved} unsolvable={unsolvable} slide_required={slidesRequired}");
            Assert.That(solved,Is.GreaterThanOrEqualTo(30));Assert.That(unsolvable,Is.GreaterThanOrEqualTo(30));Assert.That(slidesRequired,Is.GreaterThanOrEqualTo(5));
        }
        /// <summary>A larger mixed board returns a valid bounded status without deep recursion.</summary>
        [Test]public void LargerBoard()
        {var b=new Board(8,8);var random=new DeterministicRandom(741);while(b.Arrows.Count<20){int i=b.Arrows.Count;int x=(int)(random.NextFloat()*8),y=(int)(random.NextFloat()*8);b.TryAddArrow(i,x,y,(Direction)(int)(random.NextFloat()*4),i<4?ArrowKind.Ice:ArrowKind.Normal,out _);}var r=PuzzleSolver.Analyze(b,20000);Assert.That(r.NodesVisited,Is.LessThanOrEqualTo(20000));Assert.That(new[]{SolveStatus.Solved,SolveStatus.Unsolvable,SolveStatus.Unknown},Does.Contain(r.Status));}
    }
}
