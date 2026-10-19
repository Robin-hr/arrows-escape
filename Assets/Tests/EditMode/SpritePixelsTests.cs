using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Reads the actual uploaded texture pixels, including non-readable runtime textures.</summary>
    public class SpritePixelsTests
    {
        private Texture2D pixels;

        /// <summary>Copies the GPU texture so assertions inspect real pixels, not duplicate shape math.</summary>
        [SetUp]
        public void SetUp()
        {
            Texture2D source = SpriteFactory.GetArrowSprite().texture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture copy = RenderTexture.GetTemporary(source.width, source.height, 0);
            try
            {
                Graphics.Blit(source, copy);
                RenderTexture.active = copy;
                pixels = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(copy);
            }
        }

        /// <summary>Releases copies and cached runtime resources after every test.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(pixels);
            SpriteFactory.ClearCache();
        }

        /// <summary>Arrows and cell dots use separate sprites and separate textures.</summary>
        [Test]
        public void ArrowAndCellAreDifferentObjects()
        {
            Sprite arrow = SpriteFactory.GetArrowSprite();
            Sprite cell = SpriteFactory.GetCellSprite();
            Assert.That(arrow, Is.Not.SameAs(cell));
            Assert.That(arrow.texture, Is.Not.SameAs(cell.texture));
        }

        /// <summary>Every corner of the arrow's transparent background is empty.</summary>
        [Test]
        public void CornerPixelsAreTransparent()
        {
            int last = pixels.width - 1;
            foreach (Vector2Int point in new[] { new Vector2Int(0, 0), new Vector2Int(last, 0),
                new Vector2Int(0, last), new Vector2Int(last, last) })
                Assert.That(pixels.GetPixel(point.x, point.y).a, Is.Zero);
        }

        /// <summary>The upper center lies inside the arrow head.</summary>
        [Test]
        public void TipAreaIsOpaque()
        {
            Assert.That(pixels.GetPixel(64, 116).a, Is.GreaterThan(0.95f));
            Assert.That(pixels.GetPixel(40, 86).a, Is.GreaterThan(0.95f), "The head has visible shoulders.");
        }

        /// <summary>The bottom center lies inside the shaft, with empty space beside it.</summary>
        [Test]
        public void ShaftAreaIsOpaque()
        {
            Assert.That(pixels.GetPixel(64, 16).a, Is.GreaterThan(0.95f));
            Assert.That(pixels.GetPixel(40, 41).a, Is.LessThan(0.05f), "The shaft is narrower than the head.");
        }

        /// <summary>Quarter-turning the actual alpha pattern changes many pixels.</summary>
        [Test]
        public void ArrowIsNotRotationallySymmetric()
        {
            int changed = 0;
            int size = pixels.width;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (Mathf.Abs(pixels.GetPixel(x, y).a - pixels.GetPixel(y, size - 1 - x).a) > 0.5f)
                        changed++;
            Assert.That(changed, Is.GreaterThan(size * size / 10));
        }

        /// <summary>No nontransparent pixel touches the two-pixel outside border.</summary>
        [Test]
        public void ArrowPixelsHaveTransparentMargin()
        {
            for (int y = 0; y < pixels.height; y++)
                for (int x = 0; x < pixels.width; x++)
                    if (x < 2 || y < 2 || x >= pixels.width - 2 || y >= pixels.height - 2)
                        Assert.That(pixels.GetPixel(x, y).a, Is.Zero, $"Border pixel ({x}, {y})");
        }

        /// <summary>The texture is 128 square and the sprite spans exactly one world unit.</summary>
        [Test]
        public void SpriteHasRequiredSize()
        {
            Sprite sprite = SpriteFactory.GetArrowSprite();
            Assert.That(pixels.width, Is.EqualTo(128));
            Assert.That(pixels.height, Is.EqualTo(128));
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(1f).Within(0.0001f));
        }

        /// <summary>The arrow is tintable white and includes partially covered edge pixels.</summary>
        [Test]
        public void PixelsAreWhiteAndAntialiased()
        {
            int softEdges = 0;
            foreach (Color pixel in pixels.GetPixels())
            {
                if (pixel.a <= 0) continue;
                Assert.That(pixel.r, Is.EqualTo(1f).Within(0.01f));
                Assert.That(pixel.g, Is.EqualTo(1f).Within(0.01f));
                Assert.That(pixel.b, Is.EqualTo(1f).Within(0.01f));
                if (pixel.a > 0.01f && pixel.a < 0.99f) softEdges++;
            }
            Assert.That(softEdges, Is.GreaterThan(0));
        }
    }
}
