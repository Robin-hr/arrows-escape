using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Creates and caches white runtime sprites with smooth alpha edges.</summary>
    public static class SpriteFactory
    {
        private static Sprite arrowSprite;
        private static readonly Sprite[] uiSprites=new Sprite[10];
        /// <summary>Returns a cached, tintable procedural UI glyph or nine-slice surface.</summary>
        public static Sprite GetUiSprite(UiSpriteShape shape)
        {
            int index=(int)shape;
            if(uiSprites[index]==null||uiSprites[index].texture==null){DestroySprite(uiSprites[index]);uiSprites[index]=UiSpriteRaster.Create(shape);}
            return uiSprites[index];
        }

        private static Sprite cellSprite;
        private static Sprite squareSprite;
        private static Sprite iceSprite;
        private static Sprite starFilled, starOutline;
        /// <summary>Cached tintable five-point star with antialiased transparent edges.</summary>
        public static Sprite GetStarSprite(bool outline = false)
        {
            Sprite cached = outline ? starOutline : starFilled;
            if (cached != null && cached.texture != null) return cached;
            DestroySprite(cached);
            const int size = 64;
            var vertices = new Vector2[10];
            for (int i = 0; i < 10; i++)
            { float angle = Mathf.PI / 2 + i * Mathf.PI / 5; float radius = i % 2 == 0 ? .46f : .205f; vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
                {
                    var point = new Vector2((x + (sx + .5f) / 4) / size - .5f, (y + (sy + .5f) / 4) / size - .5f);
                    if (InStar(point, vertices) && (!outline || !InStar(point / .74f, vertices))) covered++;
                }
                pixels[y * size + x] = new Color(1, 1, 1, covered / 16f);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply();
            cached = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f), size);
            cached.hideFlags = HideFlags.HideAndDontSave;
            if (outline) starOutline = cached; else starFilled = cached;
            return cached;
        }
        private static bool InStar(Vector2 point, Vector2[] vertices)
        {
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((vertices[i].y > point.y) != (vertices[j].y > point.y) &&
                    point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x) inside = !inside;
            return inside;
        }
        /// <summary>Upward white crystal-cut arrow with the same outer silhouette and one-unit size.</summary>
        public static Sprite GetIceArrowSprite()
        {
            if(iceSprite==null||iceSprite.texture==null){DestroySprite(iceSprite);iceSprite=Create("Ice arrow",ViewTheme.ArrowTextureSize,true,true);}
            return iceSprite;
        }

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
            for(int i=0;i<uiSprites.Length;i++){DestroySprite(uiSprites[i]);uiSprites[i]=null;}
            DestroySprite(starFilled); DestroySprite(starOutline); starFilled = starOutline = null;
            DestroySprite(arrowSprite);
            DestroySprite(cellSprite);
            DestroySprite(squareSprite);
            DestroySprite(iceSprite);iceSprite=null;
            arrowSprite = null;
            cellSprite = null;
            squareSprite = null;
        }

        private static Sprite Create(string name, int size, bool isArrow,bool isIce=false)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color(1, 1, 1, Coverage(x, y, size, isArrow,isIce));
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static float Coverage(int x, int y, int size, bool isArrow,bool isIce=false)
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
                    if(isIce && (Mathf.Abs(py+.16f)<.13f && Mathf.Abs(px)>.035f ||
                        Mathf.Abs(Mathf.Abs(px)-.14f)+Mathf.Abs(py-.19f)<.065f))filled=false;
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
