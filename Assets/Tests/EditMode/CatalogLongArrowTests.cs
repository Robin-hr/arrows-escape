using System;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.Editor;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Real catalog validation is independent of legacy gameplay fixtures.</summary>
    public sealed class CatalogLongArrowTests
    {
        private static readonly int[] numbers=Enumerable.Range(1,360).ToArray();
        /// <summary>Every shipped board has legal long paths and retains its permanent identity.</summary>
        [TestCaseSource(nameof(numbers))]public void CatalogBoard(int number)
        {
            var paths=LevelRepository.GetLevelResourcePaths();Assert.That(paths.Count,Is.EqualTo(360));string path=paths[number-1];Assert.That(path,Does.StartWith("Levels/level_"+number.ToString("D3")));
            var result=LevelRepository.LoadFromResources(path);Assert.That(result.Success,Is.True,string.Join("; ",result.Errors));var board=result.Board;
            Assert.That(LevelSolvability.IsSolvable(board),Is.True,path);foreach(var arrow in board.Arrows)Assert.That(arrow.Length,Is.InRange(CatalogLevelRules.CatalogMinArrowLength,CatalogLevelRules.CatalogMaxArrowLength));
            if(number<=3)
            {
                Assert.That(CatalogLevelRules.ValidateStarter(board,number),Is.Empty);var shape=number==1?ShapeKind.Square:number==2?ShapeKind.Rectangle:ShapeKind.RightTriangle;var starterMask=ShapeMask.Build(shape,number==3?8:6,number==1?6:8);Assert.That(board.Arrows.Sum(a=>a.Length)*1000/starterMask.Area,Is.GreaterThanOrEqualTo(900));foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(starterMask.Contains(cell.X,cell.Y),Is.True);return;
            }
            ShapeMask mask;if(number<=60){var entry=ShapePackPlan.CreateRequests()[number-4];mask=ShapeMask.Build(entry.Shape,entry.Width,entry.Height);}else{var entry=CatalogExpansionPlan.Create()[number-61];Assert.That(entry.Number,Is.EqualTo(number));Assert.That(path,Is.EqualTo("Levels/"+entry.Id));mask=ShapeMask.Build(entry.Shape,entry.Width,entry.Height);Assert.That(LevelSolvability.CountFreeArrows(board),Is.LessThan(board.Arrows.Count));}Assert.That(board.Width,Is.LessThanOrEqualTo(24));Assert.That(board.Height,Is.LessThanOrEqualTo(36));Assert.That(board.Arrows.Sum(a=>a.Length)*1000/mask.Area,Is.GreaterThanOrEqualTo(900));
            foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(mask.Contains(cell.X,cell.Y),Is.True);for(int d=0;d<4;d++)Assert.That(board.Arrows.Count(a=>(int)a.Direction==d)*100,Is.LessThanOrEqualTo(45*board.Arrows.Count));
        }
        /// <summary>Starter scores rise strictly; the reviewed first sixty keep both neighbour bounds.</summary>
        [Test]public void SmoothnessAndStarterDifficulty()
        {
            int previous=-1;var paths=LevelRepository.GetLevelResourcePaths();for(int i=0;i<60;i++){int score=DifficultyRater.Rate(LevelAnalyzer.Analyze(LevelRepository.LoadFromResources(paths[i]).Board,new AnalysisOptions(128,32))).Score;if(i>0){Assert.That(score-previous,Is.InRange(-15,45));if(i<4)Assert.That(score,Is.GreaterThan(previous));}previous=score;}
        }
        /// <summary>Levels 61..360 repeat Easy, Medium, Hard; every board is rated inside its step (Hard may reach Expert).</summary>
        [Test]public void ExpansionCyclesEasyMediumHard()
        {
            var plan=CatalogExpansionPlan.Create();Assert.That(plan.Count,Is.EqualTo(300));var paths=LevelRepository.GetLevelResourcePaths();
            for(int i=0;i<plan.Count;i++)
            {
                var entry=plan[i];Assert.That(entry.Number,Is.EqualTo(61+i));Assert.That(entry.Tier,Is.EqualTo((DifficultyTier)(i%3)));
                int score=DifficultyRater.Rate(LevelAnalyzer.Analyze(LevelRepository.LoadFromResources(paths[60+i]).Board,new AnalysisOptions(128,32))).Score;
                if(entry.Tier==DifficultyTier.Hard)Assert.That(score,Is.GreaterThanOrEqualTo(DifficultyWeights.HardThreshold),entry.Id);
                else Assert.That(ShapeLevelGenerator.TierDistance(score,entry.Tier),Is.Zero,entry.Id+" scored "+score);
            }
        }
        /// <summary>Replacing board content never migrates or changes keyed player results.</summary>
        [Test]public void SavedKeysAndStarsStayAttached()
        {
            var tracker=new ProgressTracker();foreach(string path in LevelRepository.GetLevelResourcePaths()){string id=ProgressTracker.ToLevelId(path);tracker.RecordCompletion(id,2,1,1,1);}
            var data=UnityEngine.JsonUtility.FromJson<ProgressData>(UnityEngine.JsonUtility.ToJson(tracker.Data));var loaded=new ProgressTracker(data);
            foreach(string path in LevelRepository.GetLevelResourcePaths()){string id=ProgressTracker.ToLevelId(path);Assert.That(loaded.TryGetRecord(id,out var record),Is.True);Assert.That(record.bestMistakes,Is.EqualTo(2));Assert.That(record.bestUndos,Is.EqualTo(1));Assert.That(record.bestStars,Is.EqualTo(3));}
        }
    }
}

