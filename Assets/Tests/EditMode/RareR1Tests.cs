using System;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ArrowEscape.Tests
{
    /// <summary>Rendered dimensions, bounded layers and visibly staged signatures for the rebuilt rare recipes.</summary>
    public sealed class RareR1Tests
    {
        private static readonly string[] ids={"emerald_viper","celestial_quill","crimson_relic","chromium_spike","obsidian_shard"};
        private GameObject host;
        private LiveSkinPreview Create(string id)
        {
            host=new GameObject("Rare R1 quality fixture");var p=host.AddComponent<LiveSkinPreview>();p.Initialize(null,Vector2.zero);p.AutoReplay=false;p.SetDark(true);p.ShowStraight(id);p.SetCaptureBackground(Color.clear);p.View.Skin.SetClock(2);return p;
        }
        private static Color32[] Read(LiveSkinPreview p)
        {
            p.RenderNow();var previous=RenderTexture.active;RenderTexture.active=p.Texture;var image=new Texture2D(p.Texture.width,p.Texture.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();var data=image.GetPixels32();Object.DestroyImmediate(image);RenderTexture.active=previous;return data;
        }
        /// <summary>Owns all preview meshes, cameras and pooled emitters.</summary>
        [TearDown]public void TearDown(){if(host!=null)Object.DestroyImmediate(host);}
        /// <summary>The visible resting surface has real cell-scale weight, not just a thin highlight.</summary>
        [TestCaseSource(nameof(ids))]public void BodyWidthAtLeastPointThree(string id)
        {
            var p=Create(id);p.SetResolution(256,256);p.SetFraming(p.View.Skin.TailWorld+Vector2.right*2,.6f);
            foreach(var r in p.View.GetComponentsInChildren<Renderer>())r.enabled=r.name=="Elemental depth 0";
            var pixels=Read(p);int min=256,max=-1;for(int y=0;y<256;y++)if(pixels[y*256+128].a>128){min=Math.Min(min,y);max=Math.Max(max,y);}
            Assert.That((max-min+1)*1.2f/256,Is.GreaterThanOrEqualTo(.30f));
        }
        /// <summary>Four shared surface draws at most, also in the dense recipe.</summary>
        [TestCaseSource(nameof(ids))]public void AtMostFourDraws(string id)
        {
            var p=Create(id);foreach(bool lod in new[]{false,true}){p.SetLod(lod);int count=0;foreach(var r in p.View.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)count++;Assert.That(count,Is.LessThanOrEqualTo(4));}
        }
        /// <summary>Unequipping restores the original live classic mesh uploads and travel geometry.</summary>
        [TestCaseSource(nameof(ids))]public void ClassicMeshUpdatesAfterUnequipping(string id)
        {
            var p=Create(id);p.View.SetSkin("classic",true);p.View.Path.SetDistance(.25f);var mesh=p.View.Path.RibbonMesh;var first=mesh.vertices;
            Assert.That(first,Is.EqualTo(p.View.Path.Ribbon.Vertices));p.View.Path.SetDistance(1.25f);Assert.That(mesh.vertices,Is.EqualTo(p.View.Path.Ribbon.Vertices));Assert.That(mesh.vertices,Is.Not.EqualTo(first));
        }
        /// <summary>Each anticipation and action frame differs visibly from the pre-launch frame.</summary>
        [TestCaseSource(nameof(ids))]public void SignatureHasVisibleAnticipationAndAction(string id)
        {
            var p=Create(id);p.SetCaptureBackground(Color.black);p.SetResolution(480,270);p.SetFraming(Vector2.zero,3.3f);var rest=Read(p);p.Fly();float elapsed=0;
            foreach(float time in new[]{.07f,.14f,.22f,.32f,.44f,.58f}){p.Tick(time-elapsed);elapsed=time;var frame=Read(p);long difference=0;for(int i=0;i<frame.Length;i++)difference+=Math.Abs(frame[i].r-rest[i].r)+Math.Abs(frame[i].g-rest[i].g)+Math.Abs(frame[i].b-rest[i].b);Assert.That(difference/(frame.Length*765.0),Is.GreaterThan(.0008),id+" at "+time);}
            p.Tick(.9f-elapsed);Assert.That(p.Signatures.HasActive,Is.False);
        }
        /// <summary>Travel uses prepared buffers and stays inside the existing global particle cap.</summary>
        [TestCaseSource(nameof(ids))]public void PreparedTravelDoesNotAllocate(string id)
        {
            var p=Create(id);p.Fly();p.Tick(.01f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<700;i++)p.Tick(.001f);long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);Assert.That(SkinParticlePool.GlobalLiveCount,Is.LessThanOrEqualTo(600));
        }
        /// <summary>Both widened surfaces reuse the same fixed meshes through forward and reverse travel.</summary>
        [Test]public void SurfaceBuffersAreReusedWithoutAllocations()
        {
            var p=Create("emerald_viper");var path=p.View.Path.Ribbon;var surface=new RareRibbonSurface(path);var caps=new RareCapsSurface();
            try
            {
                var mesh=surface.Mesh;var capMesh=caps.Mesh;surface.Build(.01f);caps.Build(Vector2.right,Vector2.right,Vector2.zero,Vector2.right,true);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++){float distance=(i<50?i:100-i)*.02f;surface.Build(distance);caps.Build(new Vector2(distance+5,0),Vector2.right,new Vector2(distance,0),Vector2.right,true);}long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.That(bytes,Is.Zero);Assert.That(surface.Mesh,Is.SameAs(mesh));Assert.That(caps.Mesh,Is.SameAs(capMesh));Assert.That(caps.Mesh.vertexCount,Is.EqualTo(8));
            }finally{surface.Dispose();caps.Dispose();}
        }
        /// <summary>The edge field contains twelve separate visible glass fragments and releases its reservation.</summary>
        [Test]public void ObsidianEdgeHasTwelveVisibleFragments()
        {
            var p=Create("obsidian_shard");var center=p.View.Skin.HeadWorld;p.SetResolution(512,512);p.SetFraming(center,1.4f);p.Signatures.Edge(p.View,center);p.Signatures.Tick(.10f);
            foreach(var r in host.GetComponentsInChildren<Renderer>())r.enabled=r.enabled&&r.name=="Closing shadow rift";
            var pixels=Read(p);var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int components=0;
            for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start]||pixels[start].a<=128)continue;components++;int read=0,write=0;queue[write++]=start;seen[start]=true;
                while(read<write){int cell=queue[read++],x=cell%512,y=cell/512;Visit(cell-1,x>0);Visit(cell+1,x<511);Visit(cell-512,y>0);Visit(cell+512,y<511);Visit(cell-513,x>0&&y>0);Visit(cell-511,x<511&&y>0);Visit(cell+511,x>0&&y<511);Visit(cell+513,x<511&&y<511);}
                void Visit(int cell,bool valid){if(!valid||seen[cell]||pixels[cell].a<=128)return;seen[cell]=true;queue[write++]=cell;}
            }
            Assert.That(components,Is.EqualTo(12));Assert.That(ElementalSignatures.ActiveGlassFragments,Is.EqualTo(12));Assert.That(SkinParticlePool.GlobalLiveCount+ElementalSignatures.ActiveGlassFragments,Is.LessThanOrEqualTo(600));p.Signatures.Clear();Assert.That(ElementalSignatures.ActiveGlassFragments,Is.Zero);
        }
        /// <summary>Analytical fragments reserve native capacity and cleanup returns the full allowance.</summary>
        [Test]public void GlassFragmentsReserveAndReleaseTheSharedParticleBudget()
        {
            var p=Create("obsidian_shard");Assert.That(SkinParticlePool.NativeCapacity,Is.EqualTo(600));p.Signatures.Edge(p.View,p.View.Skin.HeadWorld);
            Assert.That(ElementalSignatures.ActiveGlassFragments,Is.EqualTo(12));Assert.That(SkinParticlePool.NativeCapacity,Is.EqualTo(588));Assert.That(SkinParticlePool.GlobalLiveCount+ElementalSignatures.ActiveGlassFragments,Is.LessThanOrEqualTo(600));
            p.Signatures.Clear();Assert.That(ElementalSignatures.ActiveGlassFragments,Is.Zero);Assert.That(SkinParticlePool.NativeCapacity,Is.EqualTo(600));
        }
        /// <summary>Destroyed Unity hosts cannot leave analytical capacity reserved by a plain C# pool.</summary>
        [Test]public void DestroyedSignatureHostReleasesItsParticleReservation()
        {
            var p=Create("obsidian_shard");var detached=new ElementalSignatures(host.transform);
            try
            {
                detached.Edge(p.View,p.View.Skin.HeadWorld);Assert.That(ElementalSignatures.ActiveGlassFragments,Is.EqualTo(12));
                Object.DestroyImmediate(host);host=null;Assert.That(ElementalSignatures.ActiveGlassFragments,Is.Zero);Assert.That(SkinParticlePool.NativeCapacity,Is.EqualTo(600));
            }finally{detached.Dispose();}
        }
    }
}
