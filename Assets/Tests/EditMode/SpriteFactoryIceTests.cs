using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Read back uploaded pixels rather than assuming the drawing code is right.</summary>
    public class SpriteFactoryIceTests
    {
        /// <summary>Checks cleanup with isolated ice-arrow state.</summary>
        [TearDown]public void Cleanup()=>SpriteFactory.ClearCache();
        private static Color[] Read(Sprite sprite)
        {
            var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(128,128,0);var pixels=new Texture2D(128,128,TextureFormat.RGBA32,false);
            try{Graphics.Blit(sprite.texture,target);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();return pixels.GetPixels();}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(pixels);}
        }
        /// <summary>Checks real pixels with isolated ice-arrow state.</summary>
        [Test]public void RealPixels()
        {
            var ice=SpriteFactory.GetIceArrowSprite();var normal=SpriteFactory.GetArrowSprite();var a=Read(ice);var b=Read(normal);
            Assert.That(ice,Is.Not.SameAs(normal));Assert.That(ice.texture,Is.Not.SameAs(normal.texture));Assert.That(ice.bounds.size.x,Is.EqualTo(1));Assert.That(ice.bounds.size.y,Is.EqualTo(1));
            int count=0,normalCount=0,rotated=0,difference=0;int minX=128,minY=128,maxX=0,maxY=0;int nMinX=128,nMinY=128,nMaxX=0,nMaxY=0;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                int i=y*128+x;
                if(a[i].a>.5f){count++;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                if(b[i].a>.5f){normalCount++;nMinX=Mathf.Min(nMinX,x);nMaxX=Mathf.Max(nMaxX,x);nMinY=Mathf.Min(nMinY,y);nMaxY=Mathf.Max(nMaxY,y);}
                if(Mathf.Abs(a[i].a-b[i].a)>.1f)difference++;
                if(Mathf.Abs(a[i].a-a[x*128+127-y].a)>.1f)rotated++;
            }
            Assert.That(count/(float)normalCount,Is.InRange(.60f,.92f));Assert.That(difference,Is.GreaterThan(100));Assert.That(rotated,Is.GreaterThan(100));
            foreach(int index in new[]{0,127,127*128,128*128-1})Assert.That(a[index].a,Is.Zero);
            Assert.That(a[100*128+64].a,Is.GreaterThan(.9f));Assert.That(a[30*128+64].a,Is.GreaterThan(.9f));
            Assert.That(Mathf.Abs(minX-nMinX)+Mathf.Abs(maxX-nMaxX)+Mathf.Abs(minY-nMinY)+Mathf.Abs(maxY-nMaxY),Is.LessThanOrEqualTo(4));
        }
        /// <summary>Checks cache with isolated ice-arrow state.</summary>
        [TestCase(false)][TestCase(true)]public void Cache(bool texture)
        {
            var first=SpriteFactory.GetIceArrowSprite();Assert.That(SpriteFactory.GetIceArrowSprite(),Is.SameAs(first));var old=first.texture;
            if(texture)Object.DestroyImmediate(old);else Object.DestroyImmediate(first);
            var next=SpriteFactory.GetIceArrowSprite();Assert.That(next!=null&&next.texture!=null,Is.True);Assert.That(next,Is.Not.SameAs(first));if(old!=null)Object.DestroyImmediate(old);
        }
    }
}

