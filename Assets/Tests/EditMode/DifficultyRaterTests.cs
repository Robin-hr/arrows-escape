using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Exact threshold boundaries and monotonic normalized integer components.</summary>
    public class DifficultyRaterTests
    {
        private static LevelAnalysis Data(int arrows=14,int choice=400,int chain=4,int traps=0,int slides=0,SolveStatus status=SolveStatus.Solved)
        {
            var path=new List<SolverMove>();for(int i=0;i<slides;i++)path.Add(new SolverMove(i,true,1,0));
            return new LevelAnalysis(8,8,arrows,0,new SolveResult(status,1,path),1,0,chain,false,0,0,0,false,0,0,0,traps,choice);
        }
        /// <summary>Boundary scores map directly to the calibrated constants.</summary>
        [Test]public void Boundaries()
        {
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.MediumThreshold-1),Is.EqualTo(DifficultyTier.Easy));
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.MediumThreshold),Is.EqualTo(DifficultyTier.Medium));
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.HardThreshold-1),Is.EqualTo(DifficultyTier.Medium));
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.HardThreshold),Is.EqualTo(DifficultyTier.Hard));
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.ExpertThreshold-1),Is.EqualTo(DifficultyTier.Hard));
            Assert.That(DifficultyRater.GetTier(DifficultyWeights.ExpertThreshold),Is.EqualTo(DifficultyTier.Expert));
        }
        /// <summary>Each increasing difficulty input cannot reduce its own component or the score.</summary>
        [Test]public void MonotonicComponents()
        {
            int size=0,scarcity=0,chain=0,traps=0,slides=0;
            for(int i=0;i<=100;i++)
            {
                var r=DifficultyRater.Rate(Data(i,1000-i*10,i,i*10,i));
                Assert.That(r.Score,Is.InRange(0,1000));Assert.That(r.Size,Is.GreaterThanOrEqualTo(size));Assert.That(r.Scarcity,Is.GreaterThanOrEqualTo(scarcity));Assert.That(r.Chain,Is.GreaterThanOrEqualTo(chain));Assert.That(r.Traps,Is.GreaterThanOrEqualTo(traps));Assert.That(r.Slides,Is.GreaterThanOrEqualTo(slides));
                size=r.Size;scarcity=r.Scarcity;chain=r.Chain;traps=r.Traps;slides=r.Slides;
            }
        }
        /// <summary>Trap risk raises the rating relative to the same solvable positions with Normal arrows.</summary>
        [Test]public void TrapPair()
        {
            var ice=LevelAnalyzer.Analyze(IceTestBoards.Trap());var normal=LevelAnalyzer.Analyze(GenerationBoards.Normal(IceTestBoards.Trap()));
            Assert.That(normal.Status,Is.EqualTo(SolveStatus.Solved));Assert.That(DifficultyRater.Rate(ice).Score,Is.GreaterThan(DifficultyRater.Rate(normal).Score));
        }
        /// <summary>Deterministic scoring refuses unsolved and absent analyses.</summary>
        [Test]public void Contracts()
        {
            Assert.That(DifficultyRater.Rate(Data()).Score,Is.EqualTo(DifficultyRater.Rate(Data()).Score));
            Assert.Throws<InvalidOperationException>(()=>DifficultyRater.Rate(Data(status:SolveStatus.Unsolvable)));
            Assert.Throws<ArgumentNullException>(()=>DifficultyRater.Rate(null));
        }
    }
}
