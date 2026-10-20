using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks streak direction, fading, bounded length and overlap.</summary>
    public class StreakPlayerTests
    {
        private EffectsTestScene scene;private EffectSpritePool pool;private StreakPlayer player;
        /// <summary>Creates an eight-slot streak player against a centered board.</summary>
        [SetUp]public void Setup(){scene=new EffectsTestScene(new Board(6,6));pool=new EffectSpritePool(scene.Host.transform,"Test streaks",8,ViewTheme.EffectsSortingOrder);player=new StreakPlayer(pool,scene.View);}
        /// <summary>Restores borrowed values and destroys owned objects.</summary>
        [TearDown]public void Cleanup()=>scene.Dispose();
        /// <summary>Each direction tracks its head and tail, stays capped and finishes.</summary>
        [TestCase(Direction.Up)][TestCase(Direction.Down)][TestCase(Direction.Left)][TestCase(Direction.Right)]public void DirectionAndFade(Direction direction)
        {
            var a=new Arrow(1,2,2,direction);Vector2 origin=scene.View.CellToWorld(2,2);player.Start(6,6,a);Assert.That(player.ActiveCount,Is.EqualTo(1));player.Tick(ViewTheme.SlideDuration*.6f);
            Assert.That(player.TryGetHeadWorld(1,out var head),Is.True);player.TryGetTailWorld(1,out var tail);Vector2 delta=head-origin;
            Assert.That(Vector2.Dot(delta,new Vector2(direction.GetDx(),direction.GetDy())),Is.GreaterThan(0));Assert.That(Vector2.Distance(head,tail),Is.LessThanOrEqualTo(ViewTheme.StreakMaxLength+.00001f));
            var sprite=pool.Get(0);Assert.That(sprite.color.a,Is.GreaterThan(0).And.LessThan(ViewTheme.StreakPeakAlpha));Assert.That(sprite.transform.localRotation,Is.EqualTo(Quaternion.identity));
            Assert.That(direction.GetDx()!=0?sprite.transform.localScale.y:sprite.transform.localScale.x,Is.EqualTo(ViewTheme.StreakThickness));
            player.Tick(ViewTheme.SlideDuration);Assert.That(player.ActiveCount,Is.Zero);Assert.That(sprite.gameObject.activeSelf,Is.False);
        }
        /// <summary>Overlapping streaks occupy independent slots; the ninth replaces the first.</summary>
        [Test]public void OverlapAndOverwrite()
        {for(int i=0;i<9;i++)player.Start(6,6,new Arrow(i,2,2,Direction.Up));Assert.That(player.ActiveCount,Is.EqualTo(8));Assert.That(player.TryGetHeadWorld(0,out _),Is.False);Assert.That(player.TryGetHeadWorld(8,out _),Is.True);Assert.That(scene.Host.transform.Find("Test streaks").childCount,Is.EqualTo(8));player.ClearAll();player.ClearAll();Assert.That(player.ActiveCount,Is.Zero);}
    }
}
