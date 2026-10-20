using System;
using System.Linq;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Full-cell occupancy and immutable path contracts.</summary>
    public class PathBoardTests
    {
        [Test] public void GroundTruth()
        {
            Board b=PathTestBoards.Load();Assert.That(b.Arrows.Where(b.CanExit).Select(a=>a.Id),Is.EqualTo(new[]{2,4,5}));
            Assert.That(b.GetBlocker(b.GetArrowById(1)).Id,Is.EqualTo(2));Assert.That(b.GetBlockingCell(b.GetArrowById(1)),Is.EqualTo(new Cell(4,0)));
            Assert.That(b.GetBlocker(b.GetArrowById(3)).Id,Is.EqualTo(1));Assert.That(b.GetBlockingCell(b.GetArrowById(3)),Is.EqualTo(new Cell(0,0)));
            Assert.That(b.GetArrowAt(1,2),Is.Null);Assert.That(b.GetArrowAt(1,3),Is.Null);Assert.That(b.GetArrowAt(4,2),Is.Null);
            Assert.That(TapResolver.Resolve(b,1,0,.6f).Id,Is.EqualTo(1));Assert.That(TapResolver.Resolve(b,1,2,.6f),Is.Null);
            Assert.That(TapResolver.Resolve(b,.5f,1,.6f).Id,Is.EqualTo(3));Assert.That(LevelSolvability.IsSolvable(b),Is.True);
        }
        [Test] public void AtomicAddAndRemove()
        {
            Board b=PathTestBoards.Load();int count=b.Arrows.Count;
            Assert.That(b.TryAddArrow(new Arrow(99,new[]{new Cell(1,2),new Cell(2,2)},Direction.Right),out _),Is.False);
            Assert.That(b.TryAddArrow(new Arrow(99,new[]{new Cell(-1,0),new Cell(0,0)},Direction.Right),out _),Is.False);Assert.That(b.Arrows.Count,Is.EqualTo(count));
            Arrow a=b.GetArrowById(4);Assert.That(b.TryMoveArrow(a,1,2,out _),Is.False);b.Remove(a);foreach(Cell c in a.Cells)Assert.That(b.GetArrowAt(c.X,c.Y),Is.Null);
        }
        [Test] public void CloneCopiesPaths()
        {Board b=PathTestBoards.Load(),c=b.Clone();Assert.That(PathTestBoards.Signature(c),Is.EqualTo(PathTestBoards.Signature(b)));Assert.That(c.Arrows[0],Is.Not.SameAs(b.Arrows[0]));c.Remove(c.Arrows[0]);Assert.That(b.Arrows.Count,Is.EqualTo(5));}
        [Test] public void InvalidPathsThrow()
        {
            Assert.Throws<ArgumentException>(()=>new Arrow(1,Array.Empty<Cell>(),Direction.Up));
            Assert.Throws<ArgumentException>(()=>new Arrow(1,new[]{new Cell(0,0),new Cell(1,1)},Direction.Up));
            Assert.Throws<ArgumentException>(()=>new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(0,0)},Direction.Left));
            Assert.Throws<ArgumentException>(()=>new Arrow(1,new[]{new Cell(0,0),new Cell(1,0)},Direction.Up));
            Assert.Throws<ArgumentException>(()=>new Arrow(1,new[]{new Cell(0,0),new Cell(1,0)},Direction.Right,ArrowKind.Ice));
        }
        [Test] public void OwnBodyBlocks()
        {
            var a=new Arrow(1,new[]{new Cell(3,1),new Cell(3,0),new Cell(2,0),new Cell(1,0),new Cell(1,1),new Cell(2,1)},Direction.Right);
            var b=new Board(5,4);b.TryAddArrow(a,out var added);Assert.That(b.CanExit(added),Is.False);Assert.That(b.GetBlocker(added),Is.SameAs(added));Assert.That(b.GetBlockingCell(added),Is.EqualTo(new Cell(3,1)));
        }
        [Test] public void HintsCountDistinctOwners()
        {
            var b=new Board(5,4);b.TryAddArrow(1,0,1,Direction.Right,out _);
            b.TryAddArrow(new Arrow(2,new[]{new Cell(1,1),new Cell(2,1),new Cell(3,1),new Cell(3,2)},Direction.Up),out _);
            var h=HintFinder.Find(b);Assert.That(h.Arrow.Id,Is.EqualTo(2));Assert.That(h.NewlyFreed,Is.EqualTo(1));Assert.That(h.Dependents,Is.EqualTo(1));
        }
    }
}
