using System;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14A regression coverage with isolated state and no player-save access.</summary>
    public sealed class StarProgressTests
    {
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void BestStarsNeverDecreaseAndTotal()
        {
            var t=new ProgressTracker();var first=t.RecordCompletion("a",2,1,20,10);
            Assert.That(first.BestStars,Is.EqualTo(1));Assert.That(first.IsNewBestStars,Is.True);
            var best=t.RecordCompletion("a",3,2,10,10);Assert.That(best.BestStars,Is.EqualTo(3));Assert.That(best.IsNewBestStars,Is.True);
            var worse=t.RecordCompletion("a",0,0,30,10);Assert.That(worse.BestStars,Is.EqualTo(3));Assert.That(worse.IsNewBestStars,Is.False);
            t.RecordCompletion("b",0,0,11,10);Assert.That(t.TotalStars,Is.EqualTo(5));
            t.TryGetRecord("a",out var record);Assert.That(record.bestMistakes,Is.Zero);Assert.That(record.bestUndos,Is.Zero);
            record.bestStars=0;Assert.That(t.TotalStars,Is.EqualTo(5));
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void VersionOneMigrationAndVersionTwoRoundTrip()
        {
            string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                string path=Path.Combine(folder,"progress.json");
                const string old="{\"version\":1,\"lastLevelId\":\"b\",\"levels\":[{\"id\":\"a\",\"completions\":4,\"bestMistakes\":2,\"bestUndos\":3},{\"id\":\"b\",\"completions\":1,\"bestMistakes\":5,\"bestUndos\":6}]}";
                File.WriteAllText(path,old);var store=new FileProgressStore(path);var loaded=store.Load();
                Assert.That(loaded.Status,Is.EqualTo(ProgressLoadStatus.Loaded));Assert.That(File.ReadAllText(path),Is.EqualTo(old));
                Assert.That(loaded.Data.lastLevelId,Is.EqualTo("b"));Assert.That(loaded.Data.levels[0].completions,Is.EqualTo(4));
                Assert.That(loaded.Data.levels[0].bestMistakes,Is.EqualTo(2));Assert.That(loaded.Data.levels[0].bestUndos,Is.EqualTo(3));
                Assert.That(loaded.Data.levels[1].completions,Is.EqualTo(1));Assert.That(loaded.Data.levels[1].bestMistakes,Is.EqualTo(5));Assert.That(loaded.Data.levels[1].bestUndos,Is.EqualTo(6));
                foreach(var record in loaded.Data.levels)Assert.That(record.bestStars,Is.Zero);
                var tracker=new ProgressTracker(loaded.Data);Assert.That(tracker.IsUnlocked(new[]{"a","b","c"},2),Is.True);
                Assert.That(store.TrySave(tracker.Data),Is.True);string json=File.ReadAllText(path);
                Assert.That(json,Does.Contain("\"version\": 2"));Assert.That(json,Does.Contain("\"bestStars\": 0"));
                Assert.That(JsonUtility.ToJson(store.Load().Data),Is.EqualTo(JsonUtility.ToJson(tracker.Data)));
                tracker.RecordCompletion("a",4,8,10,10);store.TrySave(tracker.Data);
                Assert.That(JsonUtility.ToJson(store.Load().Data),Is.EqualTo(JsonUtility.ToJson(tracker.Data)));
            }
            finally {Directory.Delete(folder,true);}
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void SingleSaveAndSandboxNoSave()
        {
            using(var scene=new ProgressTestScene())
            {
                scene.Bootstrap.ShowLevel(0);int before=scene.Store.Saves;scene.Clear();Assert.That(scene.Store.Saves,Is.EqualTo(before+1));Assert.That(scene.Store.Saved.levels[0].bestStars,Is.EqualTo(3));
                scene.Bootstrap.TryStartSandboxLevel("TestLevels/path_001");before=scene.Store.Saves;scene.Clear();Assert.That(scene.Store.Saves,Is.EqualTo(before));
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void ActualSelectStarsSitBetweenThumbnailAndNumber()
        {
            var host=new GameObject("Select",typeof(RectTransform),typeof(Canvas));
            try
            {
                host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var select=host.AddComponent<ArrowEscape.UI.LevelSelectView>();select.Build(host.transform,_=>{},()=>{});
                var tracker=new ProgressTracker();tracker.RecordCompletion("a",0,0,10,10);
                select.Refresh(LevelSelectModel.Build(new[]{"a","b","c"},tracker));Canvas.ForceUpdateCanvases();
                for(int i=0;i<3;i++)
                {
                    var tile=select.TileButtons[i];var stars=tile.GetComponentInChildren<ArrowEscape.UI.StarDisplay>(true);
                    Assert.That(stars.gameObject.activeSelf,Is.EqualTo(i==0));
                    if(i!=0)continue;
                    Assert.That(stars.Count,Is.EqualTo(3));
                    var top=new Vector3[4];var middle=new Vector3[4];var bottom=new Vector3[4];
                    tile.GetComponentInChildren<UnityEngine.UI.RawImage>().rectTransform.GetWorldCorners(top);
                    ((RectTransform)stars.transform).GetWorldCorners(middle);
                    tile.GetComponentInChildren<UnityEngine.UI.Text>().rectTransform.GetWorldCorners(bottom);
                    Assert.That(middle[2].y,Is.LessThanOrEqualTo(top[0].y));Assert.That(middle[0].y,Is.GreaterThanOrEqualTo(bottom[2].y));
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(host);}
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void SelectShowsSavedStarsOnlyForCompletedTiles()
        {
            var t=new ProgressTracker();t.RecordCompletion("a",0,0,10,10);
            var tiles=LevelSelectModel.Build(new[]{"a","b","c"},t);
            Assert.That(tiles[0].BestStars,Is.EqualTo(3));Assert.That(tiles[0].State,Is.EqualTo(LevelTileState.Completed));
            Assert.That(tiles[1].BestStars,Is.Zero);Assert.That(tiles[2].State,Is.EqualTo(LevelTileState.Locked));
        }
    }
}
