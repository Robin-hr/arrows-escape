using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Creates and caches white runtime sprites with smooth alpha edges.</summary>
    public static class SpriteFactory
    {
        private static Sprite arrowSprite;
        private static Sprite cellSprite;
        private static Sprite squareSprite;

        /// <summary>Returns the centered, one-world-unit, upward-facing arrow sprite.</summary>
        public static Sprite GetArrowSprite()
        {
            if (arrowSprite == null || arrowSprite.texture == null)
            {
                DestroySprite(arrowSprite);
                arrowSprite = Create("Arrow", ViewTheme.ArrowTextureSize, true);
            }
            return arrowSprite;
        }

        /// <summary>Returns a centered circular cell marker sprite.</summary>
        public static Sprite GetCellSprite()
        {
            if (cellSprite == null || cellSprite.texture == null)
            {
                DestroySprite(cellSprite);
                cellSprite = Create("Cell dot", ViewTheme.DotTextureSize, false);
            }
            return cellSprite;
        }

        /// <summary>Returns a cached solid white centered square, exactly one world unit wide.</summary>
        public static Sprite GetSquareSprite()
        {
            if (squareSprite == null || squareSprite.texture == null)
            {
                DestroySprite(squareSprite);
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                { name = "Guide square", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels); texture.Apply(false, true);
                squareSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);
                squareSprite.name = "Guide square"; squareSprite.hideFlags = HideFlags.HideAndDontSave;
            }
            return squareSprite;
        }

        /// <summary>Releases generated sprites and textures at the end of their lifetime.</summary>
        public static void ClearCache()
        {
            DestroySprite(arrowSprite);
            DestroySprite(cellSprite);
            DestroySprite(squareSprite);
            arrowSprite = null;
            cellSprite = null;
            squareSprite = null;
        }

        private static Sprite Create(string name, int size, bool isArrow)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color(1, 1, 1, Coverage(x, y, size, isArrow));
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static float Coverage(int x, int y, int size, bool isArrow)
        {
            int samples = ViewTheme.SamplesPerAxis;
            int inside = 0;
            for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    float px = (x + (sx + 0.5f) / samples) / size - 0.5f;
                    float py = (y + (sy + 0.5f) / samples) / size - 0.5f;
                    bool filled = isArrow ? InsideArrow(px, py) :
                        px * px + py * py <= ViewTheme.DotRadius * ViewTheme.DotRadius;
                    if (filled) inside++;
                }
            return (float)inside / (samples * samples);
        }

        private static bool InsideArrow(float x, float y)
        {
            // A vertical capsule forms the rounded shaft; a triangle forms the head.
            float nearestY = Mathf.Clamp(y, ViewTheme.ShaftBottom, ViewTheme.ShaftTop);
            float dy = y - nearestY;
            bool shaft = x * x + dy * dy <= ViewTheme.ShaftRadius * ViewTheme.ShaftRadius;
            float headWidth = ViewTheme.HeadHalfWidth *
                (ViewTheme.HeadTip - y) / (ViewTheme.HeadTip - ViewTheme.HeadBase);
            bool head = y >= ViewTheme.HeadBase && y <= ViewTheme.HeadTip && Mathf.Abs(x) <= headWidth;
            return shaft || head;
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            Texture2D texture = sprite.texture;
            DestroyObject(sprite);
            if (texture != null) DestroyObject(texture);
        }

        private static void DestroyObject(Object item)
        {
            if (Application.isPlaying) Object.Destroy(item);
            else Object.DestroyImmediate(item);
        }
    }
}
