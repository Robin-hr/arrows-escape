using System;
using System.IO;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Editor;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Pack writes are isolated to a unique temporary directory.</summary>
    public class ShapePackTests
    {
        private string folder;
        [SetUp]public void Setup()=>folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        [TearDown]public void Cleanup(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
        /// <summary>All rounds retain order, minimum requested dimensions and safe catalog suffixes.</summary>
        [Test]public void Plan()
        {
            var plan=ShapePackPlan.Create();Assert.That(plan.Count,Is.EqualTo(57));for(int i=0;i<plan.Count;i++){var e=plan[i];Assert.That(e.Number,Is.EqualTo(i+4));Assert.That(e.Width,Is.GreaterThanOrEqualTo(ShapeMask.MinimumSize(e.Shape)));Assert.That(e.Height,Is.GreaterThanOrEqualTo(ShapeMask.MinimumSize(e.Shape)));Assert.That(ShapePackBuilder.IsPackFileName(e.Id+".json"),Is.True);Assert.That(string.CompareOrdinal(e.Id,"level_003"),Is.GreaterThan(0));}var built=ShapePackBuilder.PreparePack();Assert.That(built.Count,Is.EqualTo(57));for(int i=0;i<built.Count;i++){Assert.That(built[i].Entry.Number,Is.EqualTo(i+4));if(i>0)Assert.That(built[i].Result.Rating.Score-built[i-1].Result.Rating.Score,Is.InRange(-15,45));for(int j=System.Math.Max(0,i-4);j<i;j++)Assert.That(built[i].Entry.Shape,Is.Not.EqualTo(built[j].Entry.Shape));}
        }
        /// <summary>Identical prepared boards write identical JSON and never overwrite an existing file.</summary>
        [Test]public void DeterministicWriteAndProtection()
        {
            var e=new ShapePackEntry(4,ShapeKind.Square,8,8,1,DifficultyTier.Medium);var r=ShapeLevelGenerator.Generate(new ShapeLevelSettings(ShapeKind.Square,8,8),1);Assert.That(r.Success,Is.True);var items=new[]{new ShapePackItem(e,r,0)};
            string a=Path.Combine(folder,"a"),b=Path.Combine(folder,"b");ShapePackBuilder.WriteNew(a,items);ShapePackBuilder.WriteNew(b,items);string name=e.Id+".json";Assert.That(File.ReadAllBytes(Path.Combine(a,name)),Is.EqualTo(File.ReadAllBytes(Path.Combine(b,name))));Assert.Throws<IOException>(()=>ShapePackBuilder.WriteNew(a,items));
            File.WriteAllText(Path.Combine(a,"level_001.json"),"untouched");var forbidden=new[]{new ShapePackItem(new ShapePackEntry(1,ShapeKind.Square,8,8,1,DifficultyTier.Easy),r,0)};Assert.Throws<ArgumentException>(()=>ShapePackBuilder.WriteNew(a,forbidden));Assert.That(File.ReadAllText(Path.Combine(a,"level_001.json")),Is.EqualTo("untouched"));
        }
        /// <summary>Protected originals, traversal and unrelated names cannot use the dedicated writer.</summary>
        [TestCase("level_001.json")][TestCase("level_003_shape_square.json")][TestCase("../level_004_shape_square.json")][TestCase("level_004_shape_unknown.json")]
        public void InvalidName(string name)=>Assert.That(ShapePackBuilder.IsPackFileName(name),Is.False);
        /// <summary>The original bytes stay intact while additional approved shapes may follow.</summary>
        [Test]public void Originals()=>OriginalCatalogAssertions.Check();
    }
}

