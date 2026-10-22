using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Live preview buffers stay reusable and never change economic or puzzle state.</summary>
    public sealed class LiveSkinPreviewTests
    {
        /// <summary>Warmed idle and flight Tick paths allocate no managed memory.</summary>
        [Test] public void IdleAndFlightAllocateNothing()
        {
            var host=new GameObject("preview allocation");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null);preview.Show("frostbite");preview.Tick(.01f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;i++)preview.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                preview.Fly();preview.Tick(.001f);before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;i++)preview.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                preview.SetDark(true);Assert.That(preview.IsDark,Is.True);Assert.That(preview.Texture.width,Is.EqualTo(640));
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Opening and closing a live sheet leaves inventory and the real board untouched.</summary>
        [Test] public void DetailIsVisualOnly()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Bootstrap.ShowLevel(0);int arrows=scene.Game.Session.Board.Arrows.Count,coins=scene.Bootstrap.Inventory.State.Coins,saves=scene.Inventory.Saves;
                scene.Flow.Shop.Show();scene.Flow.Shop.OpenDetail(1);Assert.That(scene.Flow.Shop.Detail.HasLivePreview,Is.True);scene.Flow.Shop.HandleBack();Assert.That(scene.Flow.Shop.Detail.HasLivePreview,Is.False);
                Assert.That(scene.Game.Session.Board.Arrows.Count,Is.EqualTo(arrows));Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.EqualTo(coins));Assert.That(scene.Inventory.Saves,Is.EqualTo(saves));
            }
        }
    }
}
