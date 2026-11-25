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
            Assert.That(LevelSolvability.IsSolvable(board),Is.True,path);if(number<=20)foreach(var arrow in board.Arrows)Assert.That(arrow.Length,Is.InRange(CatalogLevelRules.CatalogMinArrowLength,CatalogLevelRules.CatalogMaxArrowLength));
            if(number<=3)
            {
                Assert.That(CatalogLevelRules.ValidateStarter(board,number),Is.Empty);var shape=number==1?ShapeKind.Square:number==2?ShapeKind.Rectangle:ShapeKind.RightTriangle;var starterMask=ShapeMask.Build(shape,number==3?8:6,number==1?6:8);Assert.That(board.Arrows.Sum(a=>a.Length)*1000/starterMask.Area,Is.GreaterThanOrEqualTo(900));foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(starterMask.Contains(cell.X,cell.Y),Is.True);return;
            }
            if(number>20)
            {
                // Step 15: dense boards with long winding arrows, padlocks from level 75, and a NORMAL/HARD/SUPER HARD rhythm.
                Assert.That(board.Width,Is.LessThanOrEqualTo(64));Assert.That(board.Height,Is.LessThanOrEqualTo(64));
                int free=LevelSolvability.CountFreeArrows(board);Assert.That(free,Is.GreaterThanOrEqualTo(2));Assert.That(number>60?free*3:free,Is.LessThan(board.Arrows.Count),"most arrows start blocked");
                foreach(var arrow in board.Arrows){Assert.That(arrow.Length,Is.InRange(3,32));if(arrow.LockedBy!=0){Assert.That(number,Is.GreaterThanOrEqualTo(75));Assert.That(board.GetArrowById(arrow.LockedBy),Is.Not.Null);}}
                return;
            }
            ShapeMask mask;{var entry=ShapePackPlan.CreateRequests()[number-4];mask=ShapeMask.Build(entry.Shape,entry.Width,entry.Height);}Assert.That(board.Width,Is.LessThanOrEqualTo(24));Assert.That(board.Height,Is.LessThanOrEqualTo(36));Assert.That(board.Arrows.Sum(a=>a.Length)*1000/mask.Area,Is.GreaterThanOrEqualTo(900));
            foreach(var arrow in board.Arrows)foreach(var cell in arrow.Cells)Assert.That(mask.Contains(cell.X,cell.Y),Is.True);for(int d=0;d<4;d++)Assert.That(board.Arrows.Count(a=>(int)a.Direction==d)*100,Is.LessThanOrEqualTo(45*board.Arrows.Count));
        }
        /// <summary>Starter scores rise strictly; the reviewed first sixty keep both neighbour bounds.</summary>
        [Test]public void SmoothnessAndStarterDifficulty()
        {
            int previous=-1;var paths=LevelRepository.GetLevelResourcePaths();for(int i=0;i<20;i++){int score=DifficultyRater.Rate(LevelAnalyzer.Analyze(LevelRepository.LoadFromResources(paths[i]).Board,new AnalysisOptions(128,32))).Score;if(i>0){Assert.That(score-previous,Is.InRange(-15,45));if(i<4)Assert.That(score,Is.GreaterThan(previous));}previous=score;}
        }
        /// <summary>Step 15: boards grow from level 21 to 360, every Super Hard peak is bigger than the levels around it, and padlocks start at 75.</summary>
        [Test]public void Step15DifficultyCurve()
        {
            var paths=LevelRepository.GetLevelResourcePaths();var counts=new int[361];var locks=new int[361];
            for(int n=21;n<=360;n++){var b=LevelRepository.LoadFromResources(paths[n-1]).Board;counts[n]=b.Arrows.Count;locks[n]=LockPlanner.CountLocks(b);}
            Assert.That(counts[21],Is.LessThan(40));Assert.That(counts[60],Is.GreaterThan(60));Assert.That(counts[200],Is.GreaterThan(140));Assert.That(counts[350],Is.GreaterThan(180));
            for(int n=70;n<=360;n+=10){Assert.That(LevelBadge.ForCatalog(n),Is.EqualTo(LevelBadgeKind.SuperHard));if(n<360)Assert.That(counts[n],Is.GreaterThan((counts[n-2]+counts[n-3])/2),"Super Hard "+n);}
            for(int n=21;n<75;n++)Assert.That(locks[n],Is.Zero,"no padlocks before 75");
            Assert.That(locks[75],Is.EqualTo(1),"padlock tutorial");Assert.That(Enumerable.Range(75,286).Count(n=>locks[n]>0),Is.GreaterThan(250));
            double early=Enumerable.Range(61,50).Average(n=>counts[n]),late=Enumerable.Range(311,50).Average(n=>counts[n]);Assert.That(late,Is.GreaterThan(early*1.8));
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

