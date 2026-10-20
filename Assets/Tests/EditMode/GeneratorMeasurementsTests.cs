using System;
using System.Collections.Generic;
using System.Diagnostics;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.Editor;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Reproducible Editor measurements and examples; no exported assets or player save access.</summary>
    public class GeneratorMeasurementsTests
    {
        private static int largestReachable;
        private static void Observe(LevelAnalysis a){largestReachable=Math.Max(largestReachable,a.ReachableStates);}
        /// <summary>Reports every requested metric for all six hand-authored resources.</summary>
        [Test]public void ReferenceAnalyses()
        {
            foreach(string path in new[]{"Levels/level_001","Levels/level_002","Levels/level_003","TestLevels/ice_001","TestLevels/ice_002","TestLevels/ice_003"})
            {
                var a=LevelAnalyzer.Analyze(LevelRepository.LoadFromResources(path).Board);Observe(a);
                TestContext.WriteLine("REFERENCE "+path+"\n"+LevelGenerationReport.Analysis(a,DifficultyRater.Rate(a))+"\nSolver: "+LevelGenerationReport.Moves(a.SolverPath));
            }
        }
        private static void Calibration(int size,int count,int ice,bool requireSlides)
        {
            var scores=new List<int>();var tiers=new int[4];int failures=0;
            for(int seed=0;seed<50;seed++)
            {
                var result=LevelGenerator.Generate(new GeneratorSettings(size,size,count,ice,requireSlides:requireSlides),seed);
                if(!result.Success){failures++;continue;}scores.Add(result.Rating.Score);tiers[(int)result.Rating.Tier]++;Observe(result.Analysis);
            }
            scores.Sort();string range=scores.Count==0?"none":scores[0]+"/"+scores[(scores.Count-1)/2]+"/"+scores[scores.Count-1];
            TestContext.WriteLine($"CALIBRATION size={size} arrows={count} ice={ice} requireSlides={requireSlides} seeds=0..49 success={scores.Count} failed={failures} tiers={string.Join(",",tiers)} min/median/max={range}");
        }
        /// <summary>Reports strict quality failures honestly, plus optional-slide Ice profiles for useful calibration.</summary>
        [Test]public void CalibrationProfiles()
        {
            Calibration(5,8,0,true);Calibration(6,14,0,true);Calibration(7,20,1,true);Calibration(8,28,2,true);
            Calibration(7,20,1,false);Calibration(8,28,2,false);
            TestContext.WriteLine("LARGEST_REACHABLE calibration="+largestReachable);
        }
        /// <summary>Measures generation in the Unity Editor; these durations are reports, never pass/fail thresholds.</summary>
        [Test]public void EditorTiming()
        {
            foreach(var profile in new[]{new[]{6,14,0},new[]{8,28,2},new[]{10,50,0}})
            {
                var elapsed=new List<long>();int success=0;
                for(int seed=0;seed<5;seed++)
                {
                    var watch=Stopwatch.StartNew();var result=LevelGenerator.Generate(new GeneratorSettings(profile[0],profile[0],profile[1],profile[2]),seed);
                    watch.Stop();elapsed.Add(watch.ElapsedMilliseconds);if(result.Success){success++;Observe(result.Analysis);}
                }
                elapsed.Sort();TestContext.WriteLine($"EDITOR_TIME size={profile[0]} arrows={profile[1]} ice={profile[2]} attemptsLimit=40 runs=5 success={success} milliseconds={string.Join(",",elapsed)}");
            }
            TestContext.WriteLine("LARGEST_REACHABLE timing="+largestReachable);
        }
        /// <summary>Records one Easy, one Hard and one required-slide Ice example without writing JSON assets.</summary>
        [Test]public void Examples()
        {
            var settings=new[]{new GeneratorSettings(5,5,8),new GeneratorSettings(8,8,28,0,DifficultyTier.Hard),new GeneratorSettings(6,6,14,2)};
            int[] seeds={12345,0,12345};
            for(int i=0;i<settings.Length;i++)
            {
                var result=LevelGenerator.Generate(settings[i],seeds[i]);Assert.That(result.Success,Is.True);GenerationBoards.Verify(result,settings[i]);
                TestContext.WriteLine($"EXAMPLE {i+1} seed={seeds[i]} attempts={result.AttemptsUsed} matched={result.TierMatched}\n"+
                    LevelGenerationReport.Grid(result.Board)+LevelGenerationReport.Analysis(result.Analysis,result.Rating)+"\nIntended: "+LevelGenerationReport.Moves(result.IntendedSolution));
            }
        }
    }
}
