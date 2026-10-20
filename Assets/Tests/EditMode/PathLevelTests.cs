using System.Collections.Generic;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Path JSON validation and fixture quality.</summary>
    public class PathLevelTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)] public void RoundTrip(int n)
        {Board b=PathTestBoards.Load(n);var data=LevelExport.ToLevelData(b,"Round trip");Assert.That(PathTestBoards.Signature(LevelBuilder.Build(JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(data))).Board),Is.EqualTo(PathTestBoards.Signature(b)));}
        [Test] public void MissingCellsAreSingle()
        {var data=new LevelData{width=2,height=2,arrows=new List<ArrowData>{new ArrowData{id=1,x=0,y=0,direction="Up"}}};Assert.That(LevelBuilder.Build(data).Board.Arrows[0].Length,Is.EqualTo(1));}
        [Test] public void CollectsAllPathErrors()
        {
            var data=new LevelData{width=5,height=5,arrows=new List<ArrowData>{
                new ArrowData{id=1,x=4,y=4,direction="Up",type="ice",cells=new[]{0,0,2,0,0,0,-1,0,7}},
                new ArrowData{id=2,x=0,y=0,direction="Up"},
                new ArrowData{id=3,x=2,y=1,direction="Right",cells=new[]{3,1,3,0,2,0,1,0,1,1,2,1}}}};
            var r=LevelBuilder.Build(data);Assert.That(r.Success,Is.False);Assert.That(r.Board,Is.Null);string text=string.Join("|",r.Errors).ToLowerInvariant();
            foreach(string token in new[]{"odd","out of bounds","non-adjacent","repeated","head","direction","ice","arrow 2","arrow 1","own body"})Assert.That(text,Does.Contain(token));
        }
        [Test] public void FixturesMeetRequirements()
        {
            Board b=PathTestBoards.Load(2);Assert.That(b.Width,Is.EqualTo(12));Assert.That(b.Height,Is.EqualTo(12));Assert.That(b.Arrows.Count,Is.InRange(15,25));
            int bends=0;foreach(var a in b.Arrows){Assert.That(a.Length,Is.InRange(2,8));if(a.Cells.Zip(a.Cells.Skip(1),(x,y)=>new Cell(y.X-x.X,y.Y-x.Y)).Distinct().Count()>1)bends++;}
            Assert.That(bends*100,Is.GreaterThanOrEqualTo(b.Arrows.Count*40));Assert.That(LevelSolvability.CountFreeArrows(b),Is.InRange(1,4));Assert.That(LevelAnalyzer.Analyze(b,new AnalysisOptions(100)).MaxBlockerChain,Is.GreaterThanOrEqualTo(5));
            b=PathTestBoards.Load(3);Assert.That(b.Width,Is.EqualTo(24));Assert.That(b.Height,Is.EqualTo(24));Assert.That(b.Arrows.Sum(a=>a.Length)*1000/(24*24),Is.GreaterThanOrEqualTo(850));Assert.That(LevelSolvability.IsSolvable(b),Is.True);
        }
    }
}
