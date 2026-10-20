using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.Editor;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Uses the real catalog with fake stores, never the player save file.</summary>
    public class ShapeCatalogFlowTests
    {
        private GameObject host,camera;
        private GameBootstrap bootstrap;
        [TearDown]public void Cleanup(){UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(camera);SpriteFactory.ClearCache();}
        private void Create(ProgressData data)
        {
            camera=new GameObject("Catalog camera",typeof(Camera));camera.tag="MainCamera";host=new GameObject("Catalog flow");bootstrap=host.AddComponent<GameBootstrap>();bootstrap.Initialize(new FakeProgressStore{Saved=data},new FakeSettingsStore());
        }
        /// <summary>The approved pack follows the original three without replacing their keys.</summary>
        [Test]public void CountAndLast()
        {var paths=LevelRepository.GetLevelResourcePaths();Assert.That(paths.Count,Is.EqualTo(3+ShapePackPlan.Create().Count));Assert.That(paths.Skip(3),Is.EqualTo(ShapePackBuilder.OrderedResourcePaths()));Assert.That(paths.Last(),Does.StartWith("Levels/level_060_shape_"));OriginalCatalogAssertions.Check();}
        /// <summary>Real-catalog terminal flow behaves like the former third-level boundary.</summary>
        [Test]public void FinalBoundaryAndDebugWrap()
        {
            var paths=LevelRepository.GetLevelResourcePaths();var tracker=new ProgressTracker();foreach(string p in paths)tracker.RecordCompletion(ProgressTracker.ToLevelId(p),0,0);Create(tracker.Data);bootstrap.StartInitialLevel();
            int last=paths.Count-1;Assert.That(bootstrap.CurrentLevelIndex,Is.EqualTo(last));Assert.That(bootstrap.IsLastLevel,Is.True);Assert.That(bootstrap.CanGoNext,Is.False);Assert.That(bootstrap.ContinueIndex,Is.EqualTo(last));bootstrap.NextLevel();Assert.That(bootstrap.CurrentLevelIndex,Is.EqualTo(last));bootstrap.PreviousLevel();Assert.That(bootstrap.CanGoNext,Is.True);bootstrap.NextLevel();Assert.That(bootstrap.CurrentLevelIndex,Is.EqualTo(last));bootstrap.DebugJump(1);Assert.That(bootstrap.CurrentLevelIndex,Is.Zero);bootstrap.DebugJump(-1);Assert.That(bootstrap.CurrentLevelIndex,Is.EqualTo(last));
        }
        /// <summary>All 57 prepared catalog boards export byte-identically into separate temporary folders.</summary>
        [Test]public void FullPackWriterIsDeterministic()
        {
            string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            try
            {
                var items=new List<ShapePackItem>();foreach(var entry in ShapePackPlan.Create())
                {
                    var loaded=LevelRepository.LoadFromResources("Levels/"+entry.Id);Assert.That(loaded.Success,Is.True,entry.Id);
                    var analysis=LevelAnalyzer.Analyze(loaded.Board,new AnalysisOptions(1,1));var rating=DifficultyRater.Rate(analysis);
                    var result=new ShapeGenerationResult(loaded.Board,null,0,1,0,analysis,rating,false);items.Add(new ShapePackItem(entry,result,0));
                }
                string a=Path.Combine(folder,"a"),b=Path.Combine(folder,"b");ShapePackBuilder.WriteNew(a,items);ShapePackBuilder.WriteNew(b,items);
                Assert.That(Directory.GetFiles(a).Length,Is.EqualTo(57));foreach(var item in items)Assert.That(File.ReadAllBytes(Path.Combine(a,item.Entry.Id+".json")),Is.EqualTo(File.ReadAllBytes(Path.Combine(b,item.Entry.Id+".json"))));
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        /// <summary>Old completion records unlock the first added shape and keep their paired results.</summary>
        [Test]public void OriginalKeysContinueIntoPack()
        {
            var tracker=new ProgressTracker();foreach(string p in OriginalTestCatalog.Paths)tracker.RecordCompletion(ProgressTracker.ToLevelId(p),2,1);Create(tracker.Data);Assert.That(bootstrap.ContinueIndex,Is.EqualTo(3));bootstrap.StartInitialLevel();Assert.That(LevelRepository.GetLevelResourcePaths().Skip(3),Is.EqualTo(ShapePackBuilder.OrderedResourcePaths()));Assert.That(bootstrap.CurrentLevelId,Is.EqualTo(ProgressTracker.ToLevelId(ShapePackBuilder.OrderedResourcePaths()[0])));Assert.That(bootstrap.CurrentLevelId,Does.StartWith("level_004_shape_"));
            foreach(string id in new[]{"level_001","level_002","level_003"}){Assert.That(bootstrap.Progress.Tracker.TryGetRecord(id,out var record),Is.True);Assert.That(record.completions,Is.EqualTo(1));Assert.That(record.bestMistakes,Is.EqualTo(2));Assert.That(record.bestUndos,Is.EqualTo(1));}
        }
    }
}


