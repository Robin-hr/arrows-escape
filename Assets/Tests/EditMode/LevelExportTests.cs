using System;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.Editor;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Explicit kind JSON round-trips and export path protection; disk use is temporary only.</summary>
    public class LevelExportTests
    {
        private string temporary;
        /// <summary>Creates a unique temp folder outside the project.</summary>
        [SetUp]public void Setup(){temporary=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);}
        /// <summary>Deletes only the verified owned temp directory.</summary>
        [TearDown]public void Cleanup(){if(Directory.Exists(temporary))Directory.Delete(temporary,true);}
        private static void RoundTrip(Board board)
        {
            var data=LevelExport.ToLevelData(board,"Round trip");string json=JsonUtility.ToJson(data,true);
            foreach(var arrow in data.arrows)Assert.That(arrow.type,Is.EqualTo("normal").Or.EqualTo("ice"));
            Assert.That(LevelJson.TryParse(json,out var parsed,out _),Is.True);var loaded=LevelBuilder.Build(parsed);Assert.That(loaded.Success,Is.True);
            Assert.That(IceTestBoards.Key(loaded.Board),Is.EqualTo(IceTestBoards.Key(board)));Assert.That(loaded.Board.Width,Is.EqualTo(board.Width));Assert.That(loaded.Board.Height,Is.EqualTo(board.Height));
            foreach(var a in board.Arrows)Assert.That(loaded.Board.GetArrowById(a.Id).Kind,Is.EqualTo(a.Kind));
        }
        /// <summary>Ground-truth Ice and all original Normal files survive the exact existing parser/builder.</summary>
        [Test]public void KnownBoards()
        {RoundTrip(IceTestBoards.Cycle());RoundTrip(IceTestBoards.Trap());foreach(string path in LevelRepository.GetLevelResourcePaths())RoundTrip(LevelRepository.LoadFromResources(path).Board);}
        /// <summary>Fifty generated boards preserve explicit Normal and Ice kinds.</summary>
        [Test]public void GeneratedRoundTrips()
        {
            for(int seed=0;seed<50;seed++){var g=LevelGenerator.Generate(new GeneratorSettings(5,5,8,seed%3,requireSlides:false),seed);Assert.That(g.Success,Is.True);RoundTrip(g.Board);}
            var ice=LevelExport.ToLevelData(IceTestBoards.Cycle(),"Ice");string json=JsonUtility.ToJson(ice);Assert.That(json,Does.Contain("\"type\":\"ice\"").And.Contain("\"type\":\"normal\""));
        }
        /// <summary>Rejects null boards, empty names, external/traversal paths and protected resource folders.</summary>
        [Test]public void Validation()
        {
            Assert.Throws<ArgumentNullException>(()=>LevelExport.ToLevelData(null,"Name"));Assert.Throws<ArgumentException>(()=>LevelExport.ToLevelData(IceTestBoards.Cycle(),""));
            Assert.Throws<ArgumentException>(()=>LevelExport.ToLevelData(IceTestBoards.Cycle()," "));
            foreach(string path in new[]{"","../Assets","Other/Assets","C:/Assets","Assets/../outside","Assets/Resources/Levels","Assets/Resources/Levels/sub","Assets/Resources/Levels.","Assets/Resources/TestLevels ","Assets\\Resources\\TestLevels","Assets/Resources/TestLevels/child"})
                Assert.That(LevelExportPaths.Validate(path),Is.Not.Null,path);
            Assert.That(LevelExportPaths.Validate(LevelExportPaths.DefaultFolder),Is.Null);Assert.That(LevelExportPaths.Validate("Assets/Exports"),Is.Null);
        }
        /// <summary>Names include exact settings; numbered paths skip files and never overwrite.</summary>
        [Test]public void NamesAndUniqueness()
        {
            string file=LevelExportPaths.BuildFileName(6,6,DifficultyTier.Hard,12345);Assert.That(file,Is.EqualTo("gen_6x6_Hard_12345.json"));
            string first=LevelExportPaths.GetUniquePath(temporary,file);Assert.That(File.Exists(first),Is.False);File.WriteAllText(first,"keep");
            string second=LevelExportPaths.GetUniquePath(temporary,file);Assert.That(second,Does.EndWith("_2.json"));File.WriteAllText(second,"keep2");
            string third=LevelExportPaths.GetUniquePath(temporary,file);Assert.That(third,Does.EndWith("_3.json"));Assert.That(File.ReadAllText(first),Is.EqualTo("keep"));
        }
    }
}


