using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Prepared effect pools, deterministic ambient choices and allocation-free ticking.</summary>
    public sealed class SkinBudgetTests
    {
        private GameObject host;
        private BoardView board;
        private SkinDirector director;
        /// <summary>Builds an isolated visual board without touching player files.</summary>
        [SetUp] public void Setup(){host=new GameObject("skin budget");board=host.AddComponent<BoardView>();var model=new Board(8,8);for(int i=0;i<8;i++)model.TryAddArrow(i+1,i,i,Direction.Right,out _);board.Show(model);board.SetArrowSkin("obsidian_shard");director=host.GetComponent<SkinDirector>();director.SetLodOverride(false);}
        /// <summary>Disposes native objects and unregisters global pool budgets.</summary>
        [TearDown] public void Cleanup(){Object.DestroyImmediate(host);}
        /// <summary>No ambient request before 0.4 seconds and no second request inside the next interval.</summary>
        [Test] public void AmbientThrottleAndLod()
        {director.Tick(.399f);Assert.That(director.AmbientStarts,Is.Zero);director.Tick(.002f);Assert.That(director.AmbientStarts,Is.EqualTo(1));director.Tick(.39f);Assert.That(director.AmbientStarts,Is.EqualTo(1));director.SetLodOverride(true);director.Tick(5);Assert.That(director.AmbientStarts,Is.EqualTo(1));}
        /// <summary>Hard capacity holds under rapid launches; object pools never grow.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)] public void ParticleCapAndReuse(int index)
        {board.SetArrowSkin(ArrowSkinRegistry.All[index].Id);int objects=host.GetComponentsInChildren<ParticleSystem>().Length;board.TryGetArrowView(1,out var view);for(int i=0;i<150;i++)director.PlayFlight(view);Assert.That(director.LiveParticles,Is.LessThanOrEqualTo(600));Assert.That(SkinParticlePool.GlobalLiveCount,Is.LessThanOrEqualTo(600));Assert.That(director.PoolCount,Is.EqualTo(12));Assert.That(host.GetComponentsInChildren<ParticleSystem>().Length,Is.EqualTo(objects));director.Clear();Assert.That(director.LiveParticles,Is.Zero);}
        /// <summary>Skin particles plus unchanged legacy completion confetti remain within six hundred.</summary>
        [Test] public void CompletionConfettiIsReserved()
        {
            var extra=new GameObject("legacy completion reserve");SkinParticlePool pool=null;
            try
            {
                var b=extra.AddComponent<BoardView>();var model=new Board(3,3);model.TryAddArrow(1,0,0,Direction.Up,out _);b.Show(model);var effects=extra.AddComponent<EffectsController>();effects.Bind(b);pool=new SkinParticlePool(extra.transform);
                for(int i=0;i<40;i++)pool.Emit(ArrowSkinRegistry.All[0],Vector2.zero,Vector2.up,50,true);
                effects.PlayComplete(3,3,0,0);Assert.That(SkinParticlePool.NativeCapacity,Is.EqualTo(600-ViewTheme.ParticleCapacity));Assert.That(SkinParticlePool.GlobalLiveCount+effects.Particles.ActiveCount,Is.LessThanOrEqualTo(600));
            }
            finally{pool?.Dispose();Object.DestroyImmediate(extra);}
        }
        /// <summary>Warmed idle and native-particle Tick paths allocate no managed objects.</summary>
        [Test] public void TickAllocatesNothing()
        {director.Tick(.1f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;i++)director.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);board.TryGetArrowView(1,out var view);director.PlayFlight(view);director.Tick(.001f);before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;i++)director.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);}
        /// <summary>Two independent instances with the same seed choose identical ids.</summary>
        [Test] public void AmbientAndLightningDeterministic()
        {
            int[] ids=new int[10];for(int i=0;i<ids.Length;i++){director.Tick(.401f);ids[i]=director.LastAmbientId;}director.Bind(board,null);director.SetLodOverride(false);
            for(int i=0;i<ids.Length;i++){director.Tick(.401f);Assert.That(director.LastAmbientId,Is.EqualTo(ids[i]));}
            for(int f=0;f<30;f++)for(int s=0;s<10;s++)Assert.That(SkinEffectRules.Lightning(7177,s,f),Is.EqualTo(SkinEffectRules.Lightning(7177,s,f)).And.InRange(-500,500));
        }
    }
}
