using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks backward-compatible JSON type parsing and accumulated errors.</summary>
    public class LevelIceTests
    {
        /// <summary>Missing and case-insensitive types build the intended kind.</summary>
        [TestCase(null,ArrowKind.Normal)][TestCase("",ArrowKind.Normal)][TestCase("normal",ArrowKind.Normal)][TestCase("ice",ArrowKind.Ice)][TestCase("Ice",ArrowKind.Ice)][TestCase("ICE",ArrowKind.Ice)]public void Types(string type,ArrowKind kind)
        {var result=LevelBuilder.Build(new LevelData{width=4,height=4,arrows=new List<ArrowData>{new ArrowData{id=7,x=1,y=2,direction="Up",type=type}}});Assert.That(result.Success,Is.True);Assert.That(result.Board.Arrows[0].Kind,Is.EqualTo(kind));}
        /// <summary>Unknown kind reports identity and coordinates along with every other problem.</summary>
        [Test]public void Errors(){var result=LevelBuilder.Build(new LevelData{width=0,height=4,arrows=new List<ArrowData>{new ArrowData{id=7,x=-1,y=2,direction="bad",type="fire"}}});Assert.That(result.Success,Is.False);Assert.That(result.Board,Is.Null);Assert.That(result.Errors.Count,Is.EqualTo(4));Assert.That(string.Join(";",result.Errors),Does.Contain("Arrow 7 at (-1, 2)").And.Contain("unknown type"));}
        /// <summary>The real level list and its normal-arrow content remain unchanged.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)]public void OriginalLevels(int i){var r=LevelRepository.LoadFromResources("Levels/level_00"+i);Assert.That(r.Success,Is.True);Assert.That(r.Board.HasIceArrows,Is.False);foreach(var a in r.Board.Arrows)Assert.That(a.Kind,Is.EqualTo(ArrowKind.Normal));}
    }
}
