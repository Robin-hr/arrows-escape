using System.Collections.Generic;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Neon brand artwork (Resources/Brand) and the Orbitron display font (Fonts/Resources).
    /// Textures load once; sprites are created at runtime so import settings can never break the 9-slice borders.
    /// </summary>
    public static class BrandArt
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static Font display;

        /// <summary>Deep space backdrop color used behind every neon screen.</summary>
        public static Color Space => new Color32(5, 6, 26, 255);
        /// <summary>Soft lavender used for neon captions.</summary>
        public static Color Lavender => new Color32(220, 211, 255, 255);
        /// <summary>Bright cyan rim color.</summary>
        public static Color Cyan => new Color32(56, 225, 255, 255);

        /// <summary>Orbitron Black for neon headings, falling back to the UI font.</summary>
        public static Font Display
        {
            get
            {
                if (display == null) display = Resources.Load<Font>("Orbitron-Black");
                return display != null ? display : UiStyle.Font;
            }
        }

        private static Font nameFont;
        /// <summary>Blackletter profile-name font (UnifrakturCook Bold, OFL), in the style of the San Andreas logo.</summary>
        public static Font NameFont
        {
            get
            {
                if (nameFont == null) nameFont = Resources.Load<Font>("UnifrakturCook-Bold");
                return nameFont != null ? nameFont : Display;
            }
        }

        /// <summary>
        /// The player's name in the blackletter style: white letters, a heavy black outline and a thin light outer edge,
        /// so it stands out on the dark neon backgrounds.
        /// </summary>
        public static UnityEngine.UI.Text NameText(string name, Transform parent, int size, Vector2 box, Vector2 position)
        {
            var text = UiFactory.Text(name, parent, "", size, box, position);
            text.font = NameFont; text.color = Color.white; text.fontStyle = FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            var inner = text.gameObject.AddComponent<UnityEngine.UI.Outline>(); inner.effectColor = new Color(0, 0, 0, 1); inner.effectDistance = new Vector2(3, -3);
            var inner2 = text.gameObject.AddComponent<UnityEngine.UI.Outline>(); inner2.effectColor = new Color(0, 0, 0, 1); inner2.effectDistance = new Vector2(2, 2);
            var edge = text.gameObject.AddComponent<UnityEngine.UI.Outline>(); edge.effectColor = new Color(0.86f, 0.9f, 1f, 0.85f); edge.effectDistance = new Vector2(2, -2);
            return text;
        }

        /// <summary>Loads a texture from Resources/Brand, or null when it is missing.</summary>
        public static Texture2D Texture(string name)
        {
            if (textures.TryGetValue(name, out Texture2D cached) && cached != null) return cached;
            Texture2D texture = Resources.Load<Texture2D>("Brand/" + name);
            if (texture != null) { texture.wrapMode = TextureWrapMode.Clamp; textures[name] = texture; }
            return texture;
        }

        /// <summary>A full-texture sprite with optional 9-slice border (left, bottom, right, top in pixels).</summary>
        public static Sprite SpriteOf(string name, Vector4 border = default)
        {
            string key = name + border;
            if (sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Texture2D texture = Texture(name);
            if (texture == null) return null;
            Sprite sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprites[key] = sprite;
            return sprite;
        }

        /// <summary>Adds a non-interactive image; sliced when a border is given.</summary>
        public static UnityEngine.UI.Image Image(string name, Transform parent, string art, Vector2 size, Vector2 position, Vector4 border = default)
        {
            var image = UiFactory.Rect(name, parent, size, position).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = SpriteOf(art, border);
            image.type = border == Vector4.zero ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            image.preserveAspect = border == Vector4.zero;
            image.raycastTarget = false;
            if (image.sprite == null) image.color = new Color(1, 1, 1, 0);
            return image;
        }

        /// <summary>Neon text in the display font with a dark outline for readability on bright art.</summary>
        public static UnityEngine.UI.Text Text(string name, Transform parent, string value, int size, Vector2 box, Vector2 position, Color color)
        {
            var text = UiFactory.Text(name, parent, value, size, box, position);
            text.font = Display; text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.04f, 0.03f, 0.2f, 0.85f); outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        /// <summary>Makes a RawImage fill its rectangle without stretching (crops like CSS "cover").</summary>
        public static void Cover(UnityEngine.UI.RawImage image)
        {
            if (image == null || image.texture == null) return;
            Rect r = image.rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            float rectAspect = r.width / r.height, texAspect = (float)image.texture.width / image.texture.height;
            Rect uv = new Rect(0, 0, 1, 1);
            if (rectAspect > texAspect) { uv.height = texAspect / rectAspect; uv.y = (1 - uv.height) * 0.5f; }
            else { uv.width = rectAspect / texAspect; uv.x = (1 - uv.width) * 0.5f; }
            if (image.uvRect != uv) image.uvRect = uv;
        }

        /// <summary>Creates a stretched, cover-fitted background image as the first child.</summary>
        public static UnityEngine.UI.RawImage Backdrop(string name, RectTransform parent, string art)
        {
            var rect = UiFactory.Rect(name, parent, Vector2.zero, Vector2.zero); UiFactory.Stretch(rect); rect.SetAsFirstSibling();
            var image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = Texture(art); image.raycastTarget = false;
            image.color = image.texture != null ? Color.white : Space;
            Cover(image);
            return image;
        }
    }
}
