using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks particle travel, palette, determinism and pool caps.</summary>
    public class ParticlePlayerTests
    {
        /// <summary>Particles move toward their planned endpoints and fade using the palette's alpha.</summary>
        [Test]public void LifetimeAndPalette()
        {
            using(var s=new EffectsTestScene()) {
                var pool=new EffectSpritePool(s.Host.transform,"Test particles",96,2);var player=new ParticlePlayer(pool,s.View);var specs=new ParticleSpec[10];BurstPlanner.Plan(7,BurstPlannerTests.Settings(),specs);Color[] colors={Color.red,Color.green,Color.blue,Color.yellow,new Color(1,0,1,.5f)};
                player.Spawn(Vector2.one,specs,10,colors);Assert.That(player.ActiveCount,Is.EqualTo(10));for(int i=0;i<10;i++)Assert.That(pool.Get(i).color,Is.EqualTo(colors[i%5]));
                player.Tick(.15f);for(int i=0;i<10;i++){float distance=Vector2.Distance(pool.Get(i).transform.position,Vector2.one);Assert.That(distance,Is.GreaterThan(0).And.LessThan(specs[i].Distance));Assert.That(pool.Get(i).color.a,Is.LessThan(colors[i%5].a));}
                player.Tick(1);Assert.That(player.ActiveCount,Is.Zero);player.ClearAll();
            }
        }
        /// <summary>Five hundred spawns never exceed capacity; repeating the seed repeats positions.</summary>
        [Test]public void CapAndDeterminism()
        {
            using(var s=new EffectsTestScene()) {
                var pool=new EffectSpritePool(s.Host.transform,"Test particles",96,2);var player=new ParticlePlayer(pool,s.View);var b=new ParticleSpec[500];var settings=BurstPlannerTests.Settings(500);BurstPlanner.Plan(15,settings,b);player.Spawn(Vector2.zero,b,500,new[]{Color.white});
                Assert.That(player.ActiveCount,Is.EqualTo(96));Assert.That(s.Host.transform.Find("Test particles").childCount,Is.EqualTo(96));player.ClearAll();Assert.That(player.ActiveCount,Is.Zero);
                var p1=new EffectSpritePool(s.Host.transform,"A",10,2);var p2=new EffectSpritePool(s.Host.transform,"B",10,2);var a=new ParticlePlayer(p1,s.View);var c=new ParticlePlayer(p2,s.View);BurstPlanner.Plan(5,BurstPlannerTests.Settings(),b);a.Spawn(Vector2.zero,b,10,new[]{Color.white});c.Spawn(Vector2.zero,b,10,new[]{Color.white});a.Tick(.1f);c.Tick(.1f);for(int i=0;i<10;i++)Assert.That(p1.Get(i).transform.position,Is.EqualTo(p2.Get(i).transform.position));
            }
        }
    }
}
