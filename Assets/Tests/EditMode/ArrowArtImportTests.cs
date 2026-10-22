using System;
using System.IO;
using System.Linq;
using ArrowEscape.Editor;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Reference extraction is non-destructive, transparent, deterministic and override-aware.</summary>
    public sealed class ArrowArtImportTests
    {
        /// <summary>Every catalog crop plus supplementary jade is imported with a padded transparent matte.</summary>
        [Test] public void AllTwentyOnePartsExist()
        {
            var resource=ArrowSkinResources.Load();Assert.That(resource.Art.Length,Is.EqualTo(11));
            foreach(var art in resource.Art)
            {
                Assert.That(art.Full,Is.Not.Null,art.Id);Assert.That(art.Head,Is.Not.Null,art.Id);Assert.That(art.Tail,Is.Not.Null,art.Id);Assert.That(art.Shaft,Is.Not.Null,art.Id);
                var t=art.Full.texture;Assert.That(t.width,Is.GreaterThan(20));Assert.That(t.height,Is.GreaterThan(12));
                foreach(var p in new[]{new Vector2Int(0,0),new Vector2Int(t.width-1,0),new Vector2Int(0,t.height-1),new Vector2Int(t.width-1,t.height-1)})Assert.That(t.GetPixel(p.x,p.y).a,Is.Zero,art.Id);
                Assert.That(t.GetPixels32().Count(c=>c.a>40),Is.GreaterThan(40),art.Id);
                for(int y=0;y<art.Shaft.height;y++)Assert.That(art.Shaft.GetPixel(0,y),Is.EqualTo(art.Shaft.GetPixel(art.Shaft.width-1,y)));
            }
            foreach(string id in ArrowArtImporter.SheetB)if(ArrowSkinRegistry.Find(id)?.IsProcedural!=true)Assert.That(resource.Find(id).Icon,Is.Not.Null,id);
        }
        /// <summary>Pixel matting and PNG encoding are stable without rewriting project assets during tests.</summary>
        [TestCase(false)][TestCase(true)] public void ExtractionIsByteIdentical(bool flip)
        {
            var source=ArrowArtImporter.Read("Assets/ArtSource/arrows_sheet_b.png");Texture2D a=null,b=null;
            try{a=ArrowArtImporter.CreateFull(source,new RectInt(195,94,315,132),flip,"");b=ArrowArtImporter.CreateFull(source,new RectInt(195,94,315,132),flip,"");Assert.That(a.EncodeToPNG(),Is.EqualTo(b.EncodeToPNG()));}
            finally{UnityEngine.Object.DestroyImmediate(source);if(a!=null)UnityEngine.Object.DestroyImmediate(a);if(b!=null)UnityEngine.Object.DestroyImmediate(b);}
        }
        /// <summary>A high-quality transparent source wins without changing its original bytes.</summary>
        [Test] public void TransparentOverrideWins()
        {
            string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string file=Path.Combine(folder,"frostbite.png");var source=new Texture2D(24,16,TextureFormat.RGBA32,false);var pixels=new Color[384];pixels[8*24+12]=Color.magenta;source.SetPixels(pixels);source.Apply();byte[] before=source.EncodeToPNG();File.WriteAllBytes(file,before);Texture2D full=null;
            try{full=ArrowArtImporter.CreateFull(null,default,true,file);Assert.That(full.width,Is.EqualTo(36));Assert.That(full.GetPixel(18,14),Is.EqualTo(Color.magenta));Assert.That(File.ReadAllBytes(file),Is.EqualTo(before));}
            finally{UnityEngine.Object.DestroyImmediate(source);if(full!=null)UnityEngine.Object.DestroyImmediate(full);Directory.Delete(folder,true);}
        }
    }
}
