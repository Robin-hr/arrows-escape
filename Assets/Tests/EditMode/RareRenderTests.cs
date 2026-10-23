using System.Linq;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Actual shader renders verify symmetry, determinism and the frozen Inferno rest frame.</summary>
    public sealed class RareRenderTests
    {
        private static Color32[] Read(RenderTexture source)
        {var previous=RenderTexture.active;RenderTexture.active=source;var image=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();var pixels=image.GetPixels32();RenderTexture.active=previous;Object.DestroyImmediate(image);return pixels;}
        /// <summary>Same explicit clock and recipe render the same pixels after reconfiguration.</summary>
        [TestCase("obsidian_shard")][TestCase("chromium_spike")][TestCase("celestial_quill")][TestCase("emerald_viper")][TestCase("crimson_relic")]
        public void RestPixelsAreRepeatableAndStatesRestore(string id)
        {
            var host=new GameObject("Rare pixel regression");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.SetResolution(512,288);preview.ShowStraight(id);preview.View.Skin.SetClock(2);preview.RenderNow();var original=Read(preview.Texture);
                for(int i=0;i<100;i++){preview.View.SetHighlight(true);preview.View.SetHint(Color.green,1.18f);preview.View.SetFlash(Color.red,1.15f);preview.View.ClearFlash();preview.View.ClearHint();preview.View.SetHighlight(false);}preview.RenderNow();Assert.That(Read(preview.Texture),Is.EqualTo(original));
                preview.View.SetSkin("classic",true);preview.View.SetSkin(id,false);preview.View.Skin.SetClock(2);preview.RenderNow();Assert.That(Read(preview.Texture),Is.EqualTo(original));
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Warmed shared signature updates do not allocate managed memory.</summary>
        [TestCase("obsidian_shard")][TestCase("chromium_spike")][TestCase("celestial_quill")][TestCase("emerald_viper")][TestCase("crimson_relic")]
        public void PreparedExitDoesNotAllocate(string id)
        {
            var host=new GameObject("Rare allocation fixture");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.ShowStraight(id);preview.Fly();preview.Tick(.001f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<800;i++)preview.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }finally{Object.DestroyImmediate(host);}
        }
    }
}
