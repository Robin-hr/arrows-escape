using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Small runtime uGUI builders; all dimensions use Canvas reference units.</summary>
    public static class UiFactory
    {
        /// <summary>Creates a centered RectTransform under a parent.</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>Stretches a rectangle to its parent.</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Creates readable built-in-font text without intercepting pointer raycasts.</summary>
        public static UnityEngine.UI.Text Text(string name, Transform parent, string value, int fontSize, Vector2 size, Vector2 position)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<UnityEngine.UI.Text>();
            Font font = UiStyle.Font;
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = UiStyle.Ink;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        /// <summary>Creates a thumb-sized neon button with a centered display-font label.</summary>
        public static UnityEngine.UI.Button Button(string label, Transform parent, Vector2 position) => Button(label, parent, position, NeonButtonStyle.Normal);

        /// <summary>Creates a thumb-sized neon button in the given style.</summary>
        public static UnityEngine.UI.Button Button(string label, Transform parent, Vector2 position, NeonButtonStyle style)
        {
            RectTransform rect = Rect(label, parent, new Vector2(ViewTheme.ButtonWidth, ViewTheme.ButtonHeight), position);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            rect.gameObject.AddComponent<ButtonFeel>();
            button.targetGraphic = image;
            NeonKit.Style(button, style);
            var text = Text("Label", rect, label, ViewTheme.HudFontSize, Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(18, 8); text.rectTransform.offsetMax = new Vector2(-18, -8);
            NeonKit.Label(text, ViewTheme.HudFontSize - 4);
            return button;
        }

        /// <summary>Creates a full-screen view and a safe-area content rectangle.</summary>
        public static RectTransform ScreenRoot(string name, Transform parent, Color background, out RectTransform safe)
        {
            var root = Panel(name, parent, background, true);
            safe = Rect("SafeArea", root, Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaPanel>().Apply();
            ScreenAppear.Attach(root, safe);
            return root;
        }
        /// <summary>Creates a centered column with cached responsive fitting.</summary>
        public static RectTransform Column(Transform parent, float width, float height)
        {
            var rect = Rect("Column", parent, new Vector2(width, height), Vector2.zero);
            rect.gameObject.AddComponent<MenuFit>().Configure(rect, (RectTransform)parent, rect.sizeDelta);
            return rect;
        }
        /// <summary>Creates a 640 by 140 menu action.</summary>
        public static UnityEngine.UI.Button MenuButton(string label, Transform parent, float y)
        {
            var button = Button(label, parent, new Vector2(0, y));
            ((RectTransform)button.transform).sizeDelta = new Vector2(640, 140);
            return button;
        }
        /// <summary>Creates a menu title with consistent styling.</summary>
        public static UnityEngine.UI.Text Title(string text, Transform parent, float y) =>
            BrandArt.Text("Title", parent, text.ToUpperInvariant(), 60, new Vector2(720, 100), new Vector2(0, y), Color.white);
        /// <summary>Changes a toggle-style label and its normal state color.</summary>
        public static void SetToggle(UnityEngine.UI.Button button, string label, bool on)
        {
            button.GetComponentInChildren<UnityEngine.UI.Text>().text = label;
            var colors = button.colors;
            colors.normalColor = on ? Color.white : new Color(0.55f, 0.55f, 0.7f);
            button.colors = colors;
        }
        /// <summary>Creates a masked, vertically scrolling grid with fitted content height.</summary>
        public static UnityEngine.UI.ScrollRect ScrollGrid(Transform parent)
        {
            RectTransform root = Rect("ScrollView", parent, Vector2.zero, Vector2.zero);
            Stretch(root);
            root.offsetMin = new Vector2(30, 30); root.offsetMax = new Vector2(-30, -190);
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            // RectMask2D clips without stencil and culls off-screen tiles, so long lists (360 levels) batch cheaply.
            RectTransform viewport = Panel("Viewport", root, new Color(0, 0, 0, 0), true);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            RectTransform content = Rect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4; grid.cellSize = new Vector2(200, 200); grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 50;
            return scroll;
        }

        /// <summary>Creates an image panel; only a modal overlay needs to receive raycasts.</summary>
        public static RectTransform Panel(string name, Transform parent, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
            Stretch(rect);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = raycast;
            if (color == ViewTheme.PanelCard) { NeonKit.Panel(image, NeonKit.Cyan); return rect; }
            image.color = color;
            if(!raycast)UiStyle.SurfaceFor(image,false,true);
            return rect;
        }
    }
}
