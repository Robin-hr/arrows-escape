using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Exhaustive DAG counts, random playouts and authority agreement.</summary>
    public class LevelAnalyzerTests
    {
        /// <summary>Three independent exits have all eight subsets and six orders.</summary>
        [Test]public void Independent()
        {var a=LevelAnalyzer.Analyze(AudioTestScene.FreeBoard(3));Assert.That(a.ExhaustiveComplete,Is.True);Assert.That(a.SolutionCount,Is.EqualTo(6));Assert.That(a.ReachableStates,Is.EqualTo(8));Assert.That(a.DeadStates,Is.Zero);Assert.That(a.AvgChoicePermille,Is.EqualTo(1000));}
        /// <summary>Only the end of a three-arrow chain is initially legal.</summary>
        [Test]public void Chain()
        {var b=new Board(3,1);for(int x=0;x<3;x++)b.TryAddArrow(x+1,x,0,Direction.Right,out _);var a=LevelAnalyzer.Analyze(b);Assert.That(a.SolutionCount,Is.EqualTo(1));Assert.That(a.MaxBlockerChain,Is.EqualTo(3));}
        /// <summary>The ground-truth cycle requires one slide and has exactly one clearing sequence.</summary>
        [Test]public void Cycle()
        {var a=LevelAnalyzer.Analyze(IceTestBoards.Cycle());Assert.That(a.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(a.SolutionCount,Is.EqualTo(1));Assert.That(a.ReachableStates,Is.EqualTo(6));Assert.That(a.DeadStates,Is.Zero);Assert.That(a.StartMoves,Is.EqualTo(1));Assert.That(a.StartTrapMoves,Is.Zero);Assert.That(a.SlidesInSolution,Is.EqualTo(1));Assert.That(a.FreeExitsAtStart,Is.Zero);Assert.That(a.SlidesAtStart,Is.EqualTo(1));}
        /// <summary>Trap mistakes arise later, rather than directly from the initial legal actions.</summary>
        [Test]public void Trap()
        {var a=LevelAnalyzer.Analyze(IceTestBoards.Trap());Assert.That(a.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(a.DeadStates,Is.GreaterThanOrEqualTo(1));Assert.That(a.TrapMoves,Is.GreaterThanOrEqualTo(1));Assert.That(a.StartMoves,Is.EqualTo(3));Assert.That(a.StartTrapMoves,Is.Zero);Assert.That(a.TrapRatePermille,Is.GreaterThan(0));}
        /// <summary>Solvable Normal boards cannot enter traps, including real levels and one hundred seeded boards.</summary>
        [Test]public void NormalMonotonicity()
        {
            foreach(string path in LevelRepository.GetLevelResourcePaths())NoTraps(LevelRepository.LoadFromResources(path).Board);
            int checkedBoards=0;
            for(int seed=0;checkedBoards<100;seed++){var b=GenerationBoards.Normal(IceTestBoards.RandomBoard(seed));if(!LevelSolvability.IsSolvable(b))continue;NoTraps(b);checkedBoards++;}
        }
        private static void NoTraps(Board board){var a=LevelAnalyzer.Analyze(board);Assert.That(a.DeadStates,Is.Zero);Assert.That(a.TrapMoves,Is.Zero);Assert.That(a.TrapRatePermille,Is.Zero);}
        /// <summary>Unsolvable Normal boards retain actual dead-state and failed-playout measurements.</summary>
        [Test]public void DeadNormalBoard()
        {var b=new Board(2,1);b.TryAddArrow(1,0,0,Direction.Right,out _);b.TryAddArrow(2,1,0,Direction.Left,out _);var a=LevelAnalyzer.Analyze(b);Assert.That(a.DeadStates,Is.EqualTo(1));Assert.That(a.TrapRatePermille,Is.EqualTo(1000));Assert.That(a.SolutionCount,Is.Zero);}
        /// <summary>Independent exhaustive traversal agrees on three hundred meaningful seeded samples.</summary>
        [Test]public void CrossCheck()
        {
            int solved=0,dead=0,complete=0;
            for(int seed=0;seed<300;seed++)
            {
                var b=IceTestBoards.RandomBoard(seed);var a=LevelAnalyzer.Analyze(b);
                if(a.Status==SolveStatus.Solved)solved++;else if(a.Status==SolveStatus.Unsolvable)dead++;
                if(a.ExhaustiveComplete){complete++;Assert.That(a.SolutionCount>0,Is.EqualTo(a.Status==SolveStatus.Solved),"seed "+seed);}
            }
            Assert.That(solved,Is.GreaterThanOrEqualTo(30));Assert.That(dead,Is.GreaterThanOrEqualTo(30));
            TestContext.WriteLine($"ANALYZER_CROSS boards=300 solved={solved} unsolvable={dead} complete={complete}");
        }
        /// <summary>Budget exhaustion suppresses every exhaustive count while retaining sampled measurements.</summary>
        [Test]public void Incomplete()
        {var a=LevelAnalyzer.Analyze(IceTestBoards.Cycle(),new AnalysisOptions(1));Assert.That(a.ExhaustiveComplete,Is.False);Assert.That(a.ReachableStates+a.DeadStates+a.TrapMoves+a.StartMoves+a.StartTrapMoves+a.SolutionCount,Is.Zero);Assert.That(a.SolutionCountSaturated,Is.False);Assert.That(a.AvgChoicePermille,Is.GreaterThan(0));}
        /// <summary>A factorial-sized solution set is bounded or saturated, without integer overflow.</summary>
        [Test]public void Saturation()
        {var a=LevelAnalyzer.Analyze(AudioTestScene.FreeBoard(20),new AnalysisOptions(50000,1));Assert.That(a.SolutionCount,Is.InRange(0,LevelAnalyzer.SolutionLimit));Assert.That(!a.ExhaustiveComplete||a.SolutionCountSaturated,Is.True);}
        /// <summary>Results and ordering are deterministic, never mutate the board, and validate arguments.</summary>
        [Test]public void Contracts()
        {
            var b=IceTestBoards.Trap();string before=IceTestBoards.Key(b);var a=LevelAnalyzer.Analyze(b);var repeated=LevelAnalyzer.Analyze(b);
            Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));Assert.That(a.SolverPath,Is.EqualTo(repeated.SolverPath));Assert.That(ArrowEscape.Editor.LevelGenerationReport.Analysis(a,DifficultyRater.Rate(a)),Is.EqualTo(ArrowEscape.Editor.LevelGenerationReport.Analysis(repeated,DifficultyRater.Rate(repeated))));Assert.That(a.SolutionCount,Is.EqualTo(repeated.SolutionCount));Assert.That(a.TrapRatePermille,Is.EqualTo(repeated.TrapRatePermille));Assert.That(a.AvgChoicePermille,Is.EqualTo(repeated.AvgChoicePermille));
            Assert.Throws<ArgumentNullException>(()=>LevelAnalyzer.Analyze(null));Assert.Throws<ArgumentOutOfRangeException>(()=>new AnalysisOptions(0));Assert.Throws<ArgumentOutOfRangeException>(()=>new AnalysisOptions(1,0));
        }
    }
}

