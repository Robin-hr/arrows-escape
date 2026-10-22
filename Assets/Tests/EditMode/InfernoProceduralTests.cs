using System;
using System.IO;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Fire-specific procedural data and compatibility regressions.</summary>
    public sealed class InfernoProceduralTests
    {
        /// <summary>The R8 fractal repeats exactly at all borders and across cache recreation.</summary>
        [Test] public void NoiseIsDeterministicAndTileable()
        {
            var texture=InfernoTextures.Noise;Assert.That(texture.width,Is.EqualTo(256));Assert.That(texture.format,Is.EqualTo(TextureFormat.R8));byte[] data=texture.GetRawTextureData<byte>().ToArray();
            for(int i=0;i<256;i++){Assert.That(data[i*256],Is.EqualTo(data[i*256+255]));Assert.That(data[i],Is.EqualTo(data[255*256+i]));}
            InfernoTextures.Clear();Assert.That(InfernoTextures.Noise.GetRawTextureData<byte>().ToArray(),Is.EqualTo(data));
        }
        /// <summary>Auxiliary textures have transparent edges, cache and recreate deterministically.</summary>
        [Test] public void GeneratedSpotsAreCached()
        {foreach(var t in new[]{InfernoTextures.Glow,InfernoTextures.Dot,InfernoTextures.Streak}){Assert.That(t.width,Is.EqualTo(64));Assert.That(t.GetPixel(0,0).a,Is.Zero);}var old=InfernoTextures.Streak;Assert.That(InfernoTextures.Streak,Is.SameAs(old));var bytes=old.GetRawTextureData<byte>().ToArray();UnityEngine.Object.DestroyImmediate(old);Assert.That(InfernoTextures.Streak.GetRawTextureData<byte>().ToArray(),Is.EqualTo(bytes));}
        /// <summary>Exactly three upper tongues, mirrored by the SDF on the lower half.</summary>
        [Test] public void FlameHasThreeMirroredTongues()
        {
            int peaks=0;for(int i=1;i<InfernoDesign.OutlineCount-1;i++)if(InfernoDesign.OutlineY(i)>InfernoDesign.OutlineY(i-1)&&InfernoDesign.OutlineY(i)>InfernoDesign.OutlineY(i+1))peaks++;
            Assert.That(peaks,Is.EqualTo(3));Assert.That(InfernoDesign.TonguesPerSide,Is.EqualTo(3));for(int x=-10;x<=120;x+=3)for(int y=0;y<=60;y+=2)Assert.That(InfernoDesign.HeadDistance(x/100f,y/100f),Is.EqualTo(InfernoDesign.HeadDistance(x/100f,-y/100f)));
        }
        /// <summary>Speed temperature clamps and only heats up on outgoing flight.</summary>
        [Test] public void TemperatureAndColors()
        {
            Assert.That(InfernoDesign.Temperature(0),Is.Zero);Assert.That(InfernoDesign.Temperature(1),Is.EqualTo(1));float last=0;for(int i=-20;i<=120;i++){float t=InfernoDesign.Temperature(i/100f);Assert.That(t,Is.InRange(last,1));last=t;}
            Assert.That(InfernoDesign.HeatRed(0),Is.EqualTo(224/255f));Assert.That(InfernoDesign.HeatGreen(0),Is.EqualTo(64/255f));Assert.That(InfernoDesign.HeatBlue(0),Is.EqualTo(26/255f));Assert.That(InfernoDesign.HeatGreen(1),Is.EqualTo(1));Assert.That(InfernoDesign.HeatBlue(1),Is.EqualTo(1));
        }
        /// <summary>No runtime script or Resources asset references the target or retired image files.</summary>
        [Test] public void NoRuntimeReferenceTexture()
        {
            foreach(string file in Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories)){string text=File.ReadAllText(file);Assert.That(text.Contains("reference.png")||text.Contains("ArtSource/Skins")||text.Contains("Art/Arrows/inferno_streak"),Is.False,file);}
            Assert.That(ArrowSkinResources.Load().Find("inferno_streak"),Is.Null);Assert.That(Directory.Exists("Assets/Art/Arrows/inferno_streak"),Is.False);Assert.That(ArrowSkinResources.Load().Inferno,Is.Not.Null);foreach(string dependency in UnityEditor.AssetDatabase.GetDependencies("Assets/Resources/ArrowSkinResources.asset",true))Assert.That(dependency.Contains("ArtSource/Skins")||dependency.Contains("Art/Arrows/inferno_streak"),Is.False,dependency);
        }
        /// <summary>Equipping changes only the fire board; unequipping restores exact Paper.</summary>
        [Test] public void PreferredDarkAndClassicRestoration()
        {
            var cameraHost=new GameObject("Inferno test camera",typeof(Camera));cameraHost.tag="MainCamera";var selectedCamera=Camera.main;Color previousBackground=selectedCamera.backgroundColor;var host=new GameObject("Inferno board");
            try{var b=host.AddComponent<BoardView>();host.AddComponent<CameraFitter>();var model=new Board(3,3);model.TryAddArrow(1,0,0,Direction.Up,out _);b.Show(model);b.SetArrowSkin("inferno_streak");Assert.That(b.IsLightBoard,Is.False);Assert.That(selectedCamera.backgroundColor,Is.EqualTo((Color)new Color32(13,15,18,255)));host.GetComponent<SkinDirector>().SetDarkBoard(false);Assert.That(b.IsLightBoard,Is.True);b.SetArrowSkin("classic");Assert.That(selectedCamera.backgroundColor,Is.EqualTo(ViewTheme.Background));b.TryGetArrowView(1,out var view);Assert.That(view.SpriteRenderer.enabled,Is.True);Assert.That(view.BaseColor,Is.EqualTo(ViewTheme.ArrowColor));}
            finally{if(selectedCamera!=null)selectedCamera.backgroundColor=previousBackground;UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(cameraHost);}
        }
        /// <summary>Feedback does not alter the rest fire renderer or classic state adapter.</summary>
        [Test] public void HundredStateCyclesRestoreExactly()
        {
            var host=new GameObject("Inferno layers");try{var b=host.AddComponent<BoardView>();var model=new Board(3,3);model.TryAddArrow(1,1,1,Direction.Right,out _);b.Show(model);b.SetArrowSkin("inferno_streak");b.TryGetArrowView(1,out var v);var position=v.transform.position;var scale=v.transform.localScale;var color=v.SpriteRenderer.color;var fire=v.Skin.Inferno;for(int i=0;i<100;i++){v.SetHighlight(true);v.SetHint(Color.green,1.18f);v.SetFlash(Color.red,1.15f);v.ClearFlash();v.ClearHint();v.SetHighlight(false);}Assert.That(v.transform.position,Is.EqualTo(position));Assert.That(v.transform.localScale,Is.EqualTo(scale));Assert.That(v.SpriteRenderer.color,Is.EqualTo(color));Assert.That(v.Skin.Inferno,Is.SameAs(fire));}
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        /// <summary>Warm idle/flight advancement has no managed allocation and respects the global capacity.</summary>
        [Test] public void PoolBudgetLodAndAllocation()
        {
            var host=new GameObject("Inferno budget");try{var b=host.AddComponent<BoardView>();var model=new Board(10,10);for(int i=0;i<100;i++)model.TryAddArrow(i+1,i%10,i/10,Direction.Right,out _);b.Show(model);b.SetArrowSkin("inferno_streak");var d=host.GetComponent<SkinDirector>();d.SetLodOverride(false);d.Tick(.401f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)d.Tick(.001f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);for(int i=1;i<=100;i++){b.TryGetArrowView(i,out var v);d.PlayFlight(v);}Assert.That(SkinParticlePool.GlobalLiveCount,Is.LessThanOrEqualTo(600));d.Tick(.01f);before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)d.Tick(.001f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);d.Clear();b.TryGetArrowView(100,out var cleared);Assert.That(cleared.Skin.Inferno.IsFlying,Is.False);d.SetLodOverride(true);Assert.That(d.IsLod,Is.True);int count=d.AmbientStarts;d.Tick(1);Assert.That(d.AmbientStarts,Is.EqualTo(count));}
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        /// <summary>Own whoosh is deterministic, quiet and softer/shorter when blocked.</summary>
        [Test] public void ProceduralFireSound()
        {var data=SkinSoundSynth.Render(1,false);Assert.That(data,Is.EqualTo(SkinSoundSynth.Render(1,false)));Assert.That(data.Length,Is.EqualTo((int)Math.Round(.48f*44100)));Assert.That(data.Last(),Is.Zero);Assert.That(data.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)&&Math.Abs(x)<1),Is.True);Assert.That(SkinSoundSynth.Render(1,true).Length,Is.LessThan(data.Length));}
    }
}
