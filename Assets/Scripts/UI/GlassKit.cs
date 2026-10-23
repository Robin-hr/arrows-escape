using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// 3D glass finish for home elements (Step 14J): a tinted translucent glass body with a raised edge and drop
    /// shadow (Resources/Brand/Glass/*_body, greyscale so any colour works) plus an untinted gloss layer (sheen,
    /// glare streaks, specular edge and sparkle) drawn just above it, below the element's icon and label.
    /// </summary>
    public static class GlassKit
    {
        /// <summary>Glass shapes.</summary>
        public enum Kind { Tile, Pill }
        /// <summary>9-slice border of the 300 px tile art (left, bottom, right, top).</summary>
        public static readonly Vector4 TileBorder = new Vector4(64, 80, 64, 64);
        /// <summary>9-slice border of the 300x118 pill art.</summary>
        public static readonly Vector4 PillBorder = new Vector4(46, 54, 46, 44);
        /// <summary>Name of the gloss child.</summary>
        public const string GlossName = "Gloss";

        /// <summary>Element colours.</summary>
        public static readonly Color Cyan = new Color32(56, 214, 255, 255), Pink = new Color32(255, 92, 206, 255), Gold = new Color32(255, 196, 64, 255),
            Violet = new Color32(150, 112, 255, 255), Orange = new Color32(255, 136, 56, 255), Rose = new Color32(255, 88, 132, 255), Green = new Color32(52, 226, 148, 255),
            Teal = new Color32(0, 205, 190, 255), Blue = new Color32(56, 120, 255, 255);

        /// <summary>Turns <paramref name="image"/> into tinted 3D glass and adds (or reuses) its gloss layer.</summary>
        public static UnityEngine.UI.Image Apply(UnityEngine.UI.Image image, Kind kind, Color tint)
        {
            string name = kind == Kind.Tile ? "glass_tile" : "glass_pill";
            Vector4 border = kind == Kind.Tile ? TileBorder : PillBorder;
            image.sprite = BrandArt.SpriteOf("Glass/" + name + "_body", border);
            image.type = UnityEngine.UI.Image.Type.Sliced; image.preserveAspect = false;
            image.color = image.sprite != null ? tint : new Color(tint.r * 0.4f, tint.g * 0.4f, tint.b * 0.4f, 0.9f);
            Transform existing = image.transform.Find(GlossName);
            UnityEngine.UI.Image gloss = existing != null ? existing.GetComponent<UnityEngine.UI.Image>() : null;
            if (gloss == null)
            {
                gloss = UiFactory.Rect(GlossName, image.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
                UiFactory.Stretch(gloss.rectTransform); gloss.raycastTarget = false;
            }
            gloss.rectTransform.SetAsFirstSibling();
            gloss.sprite = BrandArt.SpriteOf("Glass/" + name + "_gloss", border); gloss.type = UnityEngine.UI.Image.Type.Sliced;
            gloss.color = gloss.sprite != null ? Color.white : new Color(1, 1, 1, 0);
            return gloss;
        }
    }
}
