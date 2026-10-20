using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks the real uploaded white square pixels and independent cache lifetime.</summary>
    public class SpriteFactorySquareTests
    {
        /// <summary>Releases generated resources.</summary>
        [TearDown] public void Cleanup() => SpriteFactory.ClearCache();
        /// <summary>Centered four-by-four sprites occupy exactly one unit and are cached.</summary>
        [Test] public void SizeAndCache() { var s = SpriteFactory.GetSquareSprite(); Assert.That(s, Is.SameAs(SpriteFactory.GetSquareSprite())); Assert.That(s.texture.width, Is.EqualTo(4)); Assert.That(s.texture.height, Is.EqualTo(4)); Assert.That(s.bounds.size.x, Is.EqualTo(1)); Assert.That(s.bounds.size.y, Is.EqualTo(1)); Assert.That(s.bounds.center, Is.EqualTo(Vector3.zero)); Assert.That(s.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave)); }
        /// <summary>GPU readback confirms every pixel is opaque white.</summary>
        [Test] public void Pixels()
        {
            var source = SpriteFactory.GetSquareSprite().texture; var previous = RenderTexture.active; var target = RenderTexture.GetTemporary(4, 4, 0); var pixels = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            try { Graphics.Blit(source, target); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 4, 4), 0, 0); pixels.Apply(); foreach (Color pixel in pixels.GetPixels()) { Assert.That(pixel.r, Is.EqualTo(1).Within(0.01)); Assert.That(pixel.g, Is.EqualTo(1).Within(0.01)); Assert.That(pixel.b, Is.EqualTo(1).Within(0.01)); Assert.That(pixel.a, Is.EqualTo(1)); } }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(pixels); }
        }
        /// <summary>A destroyed sprite or texture is rebuilt safely.</summary>
        [TestCase(false)][TestCase(true)] public void DestroyedCache(bool texture)
        { var first = SpriteFactory.GetSquareSprite(); var oldTexture = first.texture; if (texture) Object.DestroyImmediate(oldTexture); else Object.DestroyImmediate(first); var next = SpriteFactory.GetSquareSprite(); Assert.That(next != null && next.texture != null, Is.True); Assert.That(next, Is.Not.SameAs(first)); if (oldTexture != null) Object.DestroyImmediate(oldTexture); }
        /// <summary>Creating a cell sprite cannot replace or destroy the square cache.</summary>
        [Test] public void IndependentCache() { var square = SpriteFactory.GetSquareSprite(); var cell = SpriteFactory.GetCellSprite(); Assert.That(square != null, Is.True); Assert.That(SpriteFactory.GetSquareSprite(), Is.SameAs(square)); Assert.That(cell, Is.Not.SameAs(square)); }
        /// <summary>Explicit cleanup releases the square and its texture.</summary>
        [Test] public void Clear() { var s = SpriteFactory.GetSquareSprite(); var t = s.texture; SpriteFactory.ClearCache(); Assert.That(s == null && t == null, Is.True); }
    }
}
