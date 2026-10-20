using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Thumbnail rendering uses actual generated pixels and a strict frame budget.</summary>
    public class ThumbnailTests
    {
        private LevelThumbnailRenderer renderer;
        [SetUp]public void Setup()=>renderer=new LevelThumbnailRenderer();
        [TearDown]public void Cleanup()=>renderer.Dispose();
        private static Board Board(){var b=new Board(4,2);b.TryAddArrow(1,1,0,Direction.Up,out _);return b;}
        /// <summary>Aspect-preserving paper bounds leave transparent letterboxing.</summary>
        [Test]public void Pixels(){renderer.Request("a");renderer.Tick(_=>Board());var t=renderer.GetCached("a");Assert.That(t.width,Is.EqualTo(128));Assert.That(t.height,Is.EqualTo(128));Assert.That(t.GetPixel(64,0).a,Is.Zero);Assert.That(t.GetPixel(64,64).a,Is.EqualTo(1));}
        /// <summary>Surviving textures are reused and destroyed textures are recreated.</summary>
        [Test]public void Cache(){renderer.Request("a");renderer.Tick(_=>Board());var first=renderer.GetCached("a");renderer.Tick(_=>Board());Assert.That(renderer.GetCached("a"),Is.SameAs(first));Object.DestroyImmediate(first);renderer.Tick(_=>Board());Assert.That(renderer.GetCached("a"),Is.Not.Null);Assert.That(renderer.GetCached("a"),Is.Not.SameAs(first));}
        /// <summary>Only visible requests are rendered, at most two each tick.</summary>
        [Test]public void Budget(){for(int i=0;i<5;i++)renderer.Request(i.ToString());renderer.Tick(_=>Board());Assert.That(renderer.LastRenderCount,Is.EqualTo(2));Assert.That(renderer.GetCached("2"),Is.Null);renderer.Tick(_=>Board());Assert.That(renderer.LastRenderCount,Is.EqualTo(2));renderer.BeginFrame();renderer.Request("4");renderer.Tick(_=>Board());Assert.That(renderer.LastRenderCount,Is.EqualTo(1));Assert.That(renderer.GetCached("unrequested"),Is.Null);}
        /// <summary>Closing the screen releases every cached texture.</summary>
        [Test]public void Dispose(){renderer.Request("a");renderer.Tick(_=>Board());var t=renderer.GetCached("a");renderer.Dispose();Assert.That(t==null,Is.True);}
    }
}
