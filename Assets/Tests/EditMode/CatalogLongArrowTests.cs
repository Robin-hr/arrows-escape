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
        private static readonly int[] numbers=Enumerable.Range(1,60).ToArray();
        /// <summary>Every shipped board has legal long paths and retains its permanent identity.</summary>
        [TestCaseSource(nameof(numbers))]public void CatalogBoard(int number)
        {
            var paths=LevelRepository.GetLevelResourcePaths();Assert.That(paths.Count,Is.EqualTo(60));string path=paths[number-1];Assert.That(path,Does.StartWith("Levels/level_"+number.ToString("D3")));
            var result=LevelRepository.LoadFromResources(path);Assert.That(result.Success,Is.True,string.Join("; ",result.Errors));var board=result.Board;
            Assert.That(LevelSolvability.IsSolvable(board),Is.True,path);foreach(var arrow in board.Arrows)Assert.That(arrow.Length,Is.InRange(CatalogLevelRules.CatalogMinArrowLength,CatalogLevelRules.CatalogMaxArrowLength));
            if(number<=3)
            {
                Assert.That(CatalogLevelRules.ValidateStarter(board,number),Is.Empty);var shape=number==1?ShapeKind.Square:number==2?ShapeKind.Rectangle:ShapeKind.RightTriangle;var starterMask=ShapeMask.Build(shape,number==3?8:6,number==1?6:8);Assert.That(board.Arrows.Sum(a=>a.Length)*1000/starterMask.Area,Is.GreaterThanOrEqualTo(900));foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(starterMask.Contains(cell.X,cell.Y),Is.True);return;
            }
            var entry=ShapePackPlan.CreateRequests()[number-4];var mask=ShapeMask.Build(entry.Shape,entry.Width,entry.Height);Assert.That(board.Width,Is.LessThanOrEqualTo(24));Assert.That(board.Height,Is.LessThanOrEqualTo(36));Assert.That(board.Arrows.Sum(a=>a.Length)*1000/mask.Area,Is.GreaterThanOrEqualTo(900));
            foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(mask.Contains(cell.X,cell.Y),Is.True);for(int d=0;d<4;d++)Assert.That(board.Arrows.Count(a=>(int)a.Direction==d)*100,Is.LessThanOrEqualTo(45*board.Arrows.Count));
        }
        /// <summary>Starter scores rise strictly; the catalog keeps both neighbour bounds.</summary>
        [Test]public void SmoothnessAndStarterDifficulty()
        {
            int previous=-1;var paths=LevelRepository.GetLevelResourcePaths();for(int i=0;i<paths.Count;i++){int score=DifficultyRater.Rate(LevelAnalyzer.Analyze(LevelRepository.LoadFromResources(paths[i]).Board,new AnalysisOptions(128,32))).Score;if(i>0){Assert.That(score-previous,Is.InRange(-15,45));if(i<4)Assert.That(score,Is.GreaterThan(previous));}previous=score;}
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

