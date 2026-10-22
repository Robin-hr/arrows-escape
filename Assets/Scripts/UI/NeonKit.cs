using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Shared neon look for every screen: glassy chamfered fills with a tintable glowing rim,
    /// Orbitron labels and glowing line icons (Resources/Brand).
    /// </summary>
    public static class NeonKit
    {
        /// <summary>9-slice border of the large panel art (240 px).</summary>
        public static readonly Vector4 PanelBorder = new Vector4(60, 60, 60, 60);
        /// <summary>9-slice border of the button art (160 px).</summary>
        public static readonly Vector4 ButtonBorder = new Vector4(40, 40, 40, 40);
        /// <summary>9-slice border of the header pill art.</summary>
        public static readonly Vector4 PillBorder = new Vector4(40, 40, 40, 40);
        /// <summary>Standard cyan rim.</summary>
        public static Color Cyan => new Color32(56, 225, 255, 255);
        /// <summary>Bright rim for primary actions.</summary>
        public static Color Bright => new Color32(200, 246, 255, 255);
        /// <summary>Magenta rim for destructive actions.</summary>
        public static Color Magenta => new Color32(255, 95, 175, 255);
        /// <summary>Gold rim for completed or premium items.</summary>
        public static Color Gold => new Color32(255, 200, 60, 255);
        /// <summary>Dim violet rim for locked items.</summary>
        public static Color Dim => new Color32(110, 100, 170, 255);
        /// <summary>Readable light text on neon surfaces.</summary>
        public static Color Text => new Color32(236, 240, 255, 255);

        /// <summary>Turns an image into a neon panel (dark glass fill plus glowing rim).</summary>
        public static UnityEngine.UI.Image Panel(UnityEngine.UI.Image image, Color rim)
        {
            image.sprite = BrandArt.SpriteOf("neon_panel_fill", PanelBorder);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = image.sprite != null ? Color.white : new Color(0.08f, 0.06f, 0.28f, 0.96f);
            return SetRim(image.transform, "neon_panel_rim", PanelBorder, rim);
        }

        /// <summary>Adds a stretched, tinted neon rim child, or recolors the existing one.</summary>
        public static UnityEngine.UI.Image SetRim(Transform target, string art, Vector4 border, Color color)
        {
            Transform existing = target.Find("Rim");
            UnityEngine.UI.Image rim;
            if (existing != null) rim = existing.GetComponent<UnityEngine.UI.Image>();
            else
            {
                rim = UiFactory.Rect("Rim", target, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
                UiFactory.Stretch(rim.rectTransform); rim.rectTransform.SetAsFirstSibling(); rim.raycastTarget = false;
                rim.sprite = BrandArt.SpriteOf(art, border); rim.type = UnityEngine.UI.Image.Type.Sliced;
            }
            rim.color = rim.sprite != null ? color : new Color(0, 0, 0, 0);
            return rim;
        }

        /// <summary>Applies the neon look to a button whose target graphic is an Image.</summary>
        public static void Style(UnityEngine.UI.Button button, NeonButtonStyle style = NeonButtonStyle.Normal)
        {
            var image = (UnityEngine.UI.Image)button.targetGraphic;
            string fill = style == NeonButtonStyle.Primary ? "neon_btn_fill_cta" : style == NeonButtonStyle.Danger ? "neon_btn_fill_danger" : "neon_btn_fill";
            image.sprite = BrandArt.SpriteOf(fill, ButtonBorder); image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            if (image.sprite == null) image.color = style == NeonButtonStyle.Primary ? new Color(0.45f, 0.45f, 1f) : new Color(0.2f, 0.15f, 0.55f);
            SetRim(button.transform, "neon_btn_rim", ButtonBorder, style == NeonButtonStyle.Primary ? Bright : style == NeonButtonStyle.Danger ? Magenta : Cyan);
            var colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.8f, 0.95f); colors.disabledColor = new Color(0.42f, 0.42f, 0.56f, 0.7f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        /// <summary>Gives a button label the neon display font with best-fit sizing.</summary>
        public static void Label(UnityEngine.UI.Text text, int maxSize, Color? color = null)
        {
            if (text == null) return;
            text.font = BrandArt.Display; text.color = color ?? Text; text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = true; text.resizeTextMaxSize = maxSize; text.resizeTextMinSize = Mathf.Min(18, maxSize);
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            if (text.GetComponent<UnityEngine.UI.Outline>() == null)
            {
                var outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0.03f, 0.02f, 0.16f, 0.8f); outline.effectDistance = new Vector2(2, -2);
            }
        }

        /// <summary>Adds a glowing line icon from Resources/Brand.</summary>
        public static UnityEngine.UI.Image Icon(string name, Transform parent, string art, float size, Vector2 position)
        {
            var image = BrandArt.Image(name, parent, art, new Vector2(size, size), position);
            return image;
        }

        /// <summary>Puts an icon at the left of a button and moves its label into the remaining space.</summary>
        public static UnityEngine.UI.Image WithIcon(UnityEngine.UI.Button button, string art, float size = 64)
        {
            var rect = (RectTransform)button.transform;
            var icon = Icon("Icon", rect, art, size, Vector2.zero);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(28 + size * 0.5f, 0);
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            if (label != null)
            {
                var r = label.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(30 + size, 8); r.offsetMax = new Vector2(-22, -8);
            }
            return icon;
        }

        /// <summary>Square header button (back, close) built from the neon pill art and an icon.</summary>
        public static UnityEngine.UI.Button IconButton(string name, Transform parent, string art, Vector2 position, float size = 112, float iconSize = 64)
        {
            var image = BrandArt.Image(name, parent, "ui_pill", new Vector2(size, size), position, PillBorder);
            image.raycastTarget = true;
            if (image.sprite == null) image.color = new Color(0.16f, 0.11f, 0.48f, 0.95f);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white; colors.pressedColor = new Color(0.8f, 0.82f, 0.95f); button.colors = colors;
            image.gameObject.AddComponent<ButtonFeel>();
            Icon("Icon", button.transform, art, iconSize, Vector2.zero);
            return button;
        }

        /// <summary>Neon page backdrop (animated home art plus a shade) as the first child of a screen root.</summary>
        public static void Backdrop(RectTransform root, float shade = 0.55f)
        {
            var bg = UiFactory.Rect("NeonBackdrop", root, Vector2.zero, Vector2.zero); UiFactory.Stretch(bg); bg.SetAsFirstSibling();
            bg.gameObject.AddComponent<NeonMenuBackground>().Build(bg);
            var dim = UiFactory.Rect("Shade", bg, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(dim.rectTransform); dim.color = new Color(0.02f, 0.02f, 0.1f, shade); dim.raycastTarget = false;
        }

        /// <summary>Large Orbitron page title.</summary>
        public static UnityEngine.UI.Text Title(string text, Transform parent, Vector2 position, int size = 64, float width = 700)
            => BrandArt.Text("Title", parent, text, size, new Vector2(width, size * 1.6f), position, Color.white);
    }
}
