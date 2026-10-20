using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks additive effects lookups without changing board ownership.</summary>
    public class BoardViewEffectsApiTests
    {
        /// <summary>Released lookup disappears on discard and dot lookup safely handles missing cells.</summary>
        [Test]public void Lookups()
        {using(var s=new EffectsTestScene()){Assert.That(s.View.TryGetReleasedView(99,out _),Is.False);s.View.ReleaseArrowView(1,out var a);Assert.That(s.View.TryGetReleasedView(1,out var found),Is.True);Assert.That(found,Is.SameAs(a));s.View.DiscardReleasedView(1);Assert.That(s.View.TryGetReleasedView(1,out _),Is.False);s.View.Show(new Board(6,6));Assert.That(s.View.HasCellDots,Is.True);Assert.That(s.View.TryGetCellDot(2,3,out var dot),Is.True);Object.DestroyImmediate(dot.gameObject);Assert.That(s.View.TryGetCellDot(2,3,out _),Is.False);Assert.That(s.View.TryGetCellDot(-1,0,out _),Is.False);Assert.That(s.View.TryGetCellDot(6,0,out _),Is.False);s.View.Show(new Board(33,33));Assert.That(s.View.HasCellDots,Is.False);Assert.That(s.View.TryGetCellDot(0,0,out _),Is.False);}}
        /// <summary>Sorting layers keep every intended relative order, without accidental collisions.</summary>
        [Test]public void SortingLayers()
        {int[] orders={ViewTheme.CellSortingOrder,ViewTheme.ArrowSortingOrder,ViewTheme.EffectsSortingOrder,ViewTheme.GuideSortingOrder,ViewTheme.SlidingSortingOrder};Assert.That(orders,Is.EqualTo(new[]{0,1,2,3,4}));Assert.That(orders,Is.Unique);}
    }
}
