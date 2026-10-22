using System.Linq;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Actual shader renders verify symmetry, determinism and the frozen Inferno rest frame.</summary>
    public sealed class ElementalRenderTests
    {
        private static Color32[] Read(RenderTexture source)
        {var previous=RenderTexture.active;RenderTexture.active=source;var image=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();var pixels=image.GetPixels32();RenderTexture.active=previous;Object.DestroyImmediate(image);return pixels;}
        /// <summary>Same explicit clock and recipe render the same pixels after reconfiguration.</summary>
        [TestCase("frostbite")][TestCase("plasma_bolt")][TestCase("void_whisper")][TestCase("inferno_streak")]
        public void RestPixelsAreRepeatableAndStatesRestore(string id)
        {
            var host=new GameObject("Elemental pixel regression");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.SetResolution(512,288);preview.ShowStraight(id);preview.View.Skin.SetClock(2);preview.RenderNow();var original=Read(preview.Texture);
                for(int i=0;i<100;i++){preview.View.SetHighlight(true);preview.View.SetHint(Color.green,1.18f);preview.View.SetFlash(Color.red,1.15f);preview.View.ClearFlash();preview.View.ClearHint();preview.View.SetHighlight(false);}preview.RenderNow();Assert.That(Read(preview.Texture),Is.EqualTo(original));
                preview.View.SetSkin("classic",true);preview.View.SetSkin(id,false);preview.View.Skin.SetClock(2);preview.RenderNow();Assert.That(Read(preview.Texture),Is.EqualTo(original));
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Mirrored head silhouettes remain symmetric independently of internal moving detail.</summary>
        [TestCase("frostbite")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void ProceduralHeadSilhouetteIsMirrored(string id)
        {
            var host=new GameObject("Head mirror render");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();Vector2 origin=Vector2.zero;preview.Initialize(null,origin);preview.AutoReplay=false;preview.SetDark(true);preview.ShowStraight(id);preview.SetResolution(256,256);preview.SetFraming(preview.View.Skin.HeadWorld-new Vector2(.55f,0),.85f);preview.SetCaptureBackground(Color.clear);preview.View.Skin.SetClock(0);
                foreach(var renderer in host.GetComponentsInChildren<Renderer>())renderer.enabled=renderer.name=="Elemental SDF head";preview.RenderNow();var data=Read(preview.Texture);int occupied=0;
                for(int y=0;y<128;y++){int upper=0,lower=0;for(int x=0;x<256;x++){if(data[y*256+x].a>128)lower++;if(data[(255-y)*256+x].a>128)upper++;}occupied+=upper+lower;Assert.That(upper,Is.EqualTo(lower).Within(2),id+" row "+y);}Assert.That(occupied,Is.GreaterThan(100));
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>All three recipes reuse deterministic generated noise and never import bitmaps.</summary>
        [Test] public void GeneratedNoiseCacheIsDeterministic()
        {
            var original=InfernoTextures.Noise.GetRawTextureData<byte>().ToArray();Object.DestroyImmediate(InfernoTextures.Noise);Assert.That(InfernoTextures.Noise.GetRawTextureData<byte>().ToArray(),Is.EqualTo(original));for(int i=0;i<256;i++){Assert.That(original[i],Is.EqualTo(original[255*256+i]));Assert.That(original[i*256],Is.EqualTo(original[i*256+255]));}
        }
        /// <summary>LOD keeps the thin shadow path legible after optional smoke is disabled.</summary>
        [Test] public void DenseVoidCenterlineMeetsContrast()
        {
            var host=new GameObject("Dense Void contrast");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.ShowStraight("void_whisper");preview.SetResolution(512,288);preview.SetLod(true);preview.View.Skin.SetClock(0);preview.RenderNow();var pixels=Read(preview.Texture);Color background=pixels[0];
                foreach(int x in new[]{220,256,292})Assert.That(ArrowTint.Contrast(pixels[144*512+x],background),Is.GreaterThanOrEqualTo(3),"centerline x="+x);
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>The shop demonstration includes the edge bite and uses only prepared resources.</summary>
        [Test] public void PreviewBitesTheEdgeOnceWithoutAllocating()
        {
            var host=new GameObject("Frost preview edge");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.ShowStraight("frostbite");preview.Fly();preview.Tick(.001f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<800;i++)preview.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(preview.Signatures.EdgeStarts,Is.EqualTo(1));
            }finally{Object.DestroyImmediate(host);}
        }
    }
}
