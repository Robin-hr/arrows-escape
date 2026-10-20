using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks immutable replacements, gaps and membership without changing normal rules.</summary>
    public class BoardIceTests
    {
        /// <summary>Default kind, gaps, sliding eligibility and the old blocking rules.</summary>
        [Test]public void GapsAndKinds()
        {var b=IceTestBoards.Cycle();Assert.That(new Arrow(9,0,0,Direction.Up).Kind,Is.EqualTo(ArrowKind.Normal));Assert.That(b.HasIceArrows,Is.True);int[] gaps={2,1,2,1};for(int id=1;id<=4;id++){Arrow a=b.GetArrowById(id);Assert.That(b.CountFreeCellsAhead(a),Is.EqualTo(gaps[id-1]));Assert.That(b.CanExit(a),Is.False);Assert.That(b.GetBlocker(a),Is.Not.Null);Assert.That(b.CanSlide(a),Is.EqualTo(id==1));}Assert.That(b.CountFreeCellsAhead(new Arrow(99,0,0,Direction.Right)),Is.Zero);b.TryMoveArrow(b.GetArrowById(1),2,0,out var moved);Assert.That(b.CountFreeCellsAhead(moved),Is.Zero);Assert.That(b.CanSlide(moved),Is.False);var clear=new Board(4,4);clear.TryAddArrow(1,1,1,Direction.Up,ArrowKind.Ice,out var free);Assert.That(clear.CountFreeCellsAhead(free),Is.EqualTo(2));Assert.That(clear.CanExit(free),Is.True);Assert.That(clear.CanSlide(free),Is.False);}
        /// <summary>Moves preserve kind and list index, and clones remain independent.</summary>
        [Test]public void ReplaceAndClone()
        {var b=IceTestBoards.Cycle();Arrow old=b.Arrows[0];Board clone=b.Clone();Assert.That(b.TryMoveArrow(old,2,0,out var moved),Is.True);Assert.That(moved.Id,Is.EqualTo(old.Id));Assert.That(moved.Direction,Is.EqualTo(old.Direction));Assert.That(moved.Kind,Is.EqualTo(ArrowKind.Ice));Assert.That(b.Arrows[0],Is.SameAs(moved));Assert.That(b.GetArrowAt(0,0),Is.Null);Assert.That(b.GetArrowAt(2,0),Is.SameAs(moved));Assert.That(clone.GetArrowById(1).X,Is.Zero);Assert.That(clone.GetArrowById(1).Kind,Is.EqualTo(ArrowKind.Ice));Assert.That(b.TryMoveArrow(old,1,0,out _),Is.False);Assert.That(b.CanExit(old),Is.False);Assert.That(b.GetBlocker(old),Is.Null);}
        /// <summary>Invalid target cells and unknown instances fail without throwing.</summary>
        [TestCase(-1,0)][TestCase(4,0)][TestCase(3,0)][TestCase(0,0)]public void InvalidMove(int x,int y)
        {var b=IceTestBoards.Cycle();string before=IceTestBoards.Key(b);Assert.That(b.TryMoveArrow(b.GetArrowById(1),x,y,out var moved),Is.False);Assert.That(moved,Is.Null);Assert.That(IceTestBoards.Key(b),Is.EqualTo(before));Assert.That(b.TryMoveArrow(new Arrow(99,0,0,Direction.Right),1,0,out _),Is.False);Assert.That(b.TryMoveArrow(null,1,0,out _),Is.False);}
        /// <summary>Ice membership disappears when the final ice arrow exits.</summary>
        [Test]public void IceMembership(){var b=IceTestBoards.Cycle();b.Remove(b.GetArrowById(1));Assert.That(b.HasIceArrows,Is.False);}
    }
}
