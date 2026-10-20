using System;
using System.IO;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Editor;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks the staged builder output through real JSON files in a temporary folder.</summary>
    public class ShapePackAreaTests
    {
        private string folder;
        private System.Collections.Generic.IReadOnlyList<ShapePackItem> pack;
        [OneTimeSetUp]public void Prepare()
        {
            folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            pack=ShapePackBuilder.PreparePack();ShapePackBuilder.WriteNew(folder,pack);
        }
        [OneTimeTearDown]public void Cleanup(){if(folder!=null&&Directory.Exists(folder))Directory.Delete(folder,true);}
        [Test]public void FilesAreConsecutiveAndOpeningIsGentle()
        {
            var files=Directory.GetFiles(folder,"*.json").Select(Path.GetFileName).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            Assert.That(files.Length,Is.EqualTo(57));
            for(int i=0;i<57;i++){Assert.That(files[i],Is.EqualTo(pack[i].Entry.Id+".json"));Assert.That(pack[i].Entry.Number,Is.EqualTo(i+4));}
            Assert.That(pack[0].Result.Rating.Score,Is.LessThanOrEqualTo(230));
        }
        [Test]public void NeighboursMeetBothSmoothnessBounds()
        {for(int i=1;i<pack.Count;i++)Assert.That(pack[i].Result.Rating.Score-pack[i-1].Result.Rating.Score,Is.InRange(-15,45),pack[i].Entry.Id);}
        [Test]public void FourInterveningLevelsSeparateEveryRepeatedShape()
        {for(int i=0;i<pack.Count;i++)for(int j=Math.Max(0,i-4);j<i;j++)Assert.That(pack[i].Entry.Shape,Is.Not.EqualTo(pack[j].Entry.Shape));}
        [Test]public void ExportedBoardsAreSolvableCoveredAndPhoneSized()
        {
            foreach(var item in pack)
            {
                string json=File.ReadAllText(Path.Combine(folder,item.Entry.Id+".json"));
                Assert.That(ArrowEscape.Data.LevelJson.TryParse(json,out var data,out var error),Is.True,error);
                var loaded=LevelBuilder.Build(data);Assert.That(loaded.Success,Is.True);
                Assert.That(loaded.Board.Width,Is.LessThanOrEqualTo(24));Assert.That(loaded.Board.Height,Is.LessThanOrEqualTo(36));
                Assert.That(LevelSolvability.IsSolvable(loaded.Board),Is.True);
                Assert.That(loaded.Board.Arrows.Sum(a=>a.Length)*1000/item.Result.Mask.Area,Is.GreaterThanOrEqualTo(900));
                Assert.That(DifficultyRater.Rate(LevelAnalyzer.Analyze(loaded.Board,new AnalysisOptions(128,32))).Score,Is.EqualTo(item.Result.Rating.Score));
            }
        }
        [Test]public void AreaBandsRespectMinimumAndPhoneExceptions()
        {
            foreach(var e in ShapePackPlan.CreateRequests())
            {
                int area=ShapeMask.Build(e.Shape,e.Width,e.Height).Area;
                Cell min=ShapeMask.DefaultSize(e.Shape,ShapeMask.MinimumSize(e.Shape));int minimum=ShapeMask.Build(e.Shape,min.X,min.Y).Area;
                if(minimum*100>e.TargetArea*112)Assert.That(area,Is.EqualTo(minimum));
                else if(area*100<e.TargetArea*88)
                {
                    Cell next=ShapeMask.DefaultSize(e.Shape,Math.Min(e.Width,e.Height)+1);
                    Assert.That(next.X>24||next.Y>36,Is.True,"Only the phone cap can force an undersized band.");
                }
                else Assert.That(area*100,Is.InRange(e.TargetArea*88,e.TargetArea*112));
            }
        }
        [Test]public void EveryShapeHasExactlyThreeBands()
        {foreach(ShapeKind shape in Enum.GetValues(typeof(ShapeKind)))Assert.That(pack.Where(i=>i.Entry.Shape==shape).Select(i=>i.Entry.TargetArea).OrderBy(x=>x),Is.EqualTo(new[]{70,180,330}));}
    }
}
