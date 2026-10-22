using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>The home page's neon space backdrop (light rays, stars, grid floor and faint arrows), cover-fitted.</summary>
    public sealed class NeonMenuBackground : MonoBehaviour, IMenuBackground
    {
        private UnityEngine.UI.RawImage image;
        private Vector2 lastSize;

        /// <summary>The backdrop image.</summary>
        public UnityEngine.UI.RawImage Image => image;

        /// <summary>Creates the backdrop as the first child of the menu root.</summary>
        public void Build(RectTransform parent)
        {
            // Static base (sky, glow, stars, horizon) + animated warp streaks and scrolling grid + decorative arrows.
            bool layered = BrandArt.Texture("home_base") != null;
            image = BrandArt.Backdrop("NeonBackground", parent, layered ? "home_base" : "home_background");
            if (!layered) return;
            var warpRect = UiFactory.Rect("Warp", parent, Vector2.zero, Vector2.zero); UiFactory.Stretch(warpRect); warpRect.SetSiblingIndex(image.transform.GetSiblingIndex() + 1);
            Warp = warpRect.gameObject.AddComponent<WarpBackground>(); Warp.Bind(image); NeonKit.Isolate(warpRect.gameObject);
            arrows = UiFactory.Rect("Arrows", parent, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            UiFactory.Stretch(arrows.rectTransform); arrows.rectTransform.SetSiblingIndex(warpRect.GetSiblingIndex() + 1);
            arrows.texture = BrandArt.Texture("home_arrows"); arrows.raycastTarget = false; if (arrows.texture == null) arrows.enabled = false;
        }

        /// <summary>The animated warp/grid layer, or null when the layered art is missing.</summary>
        public WarpBackground Warp { get; private set; }
        private UnityEngine.UI.RawImage arrows;

        /// <summary>The backdrop is static; nothing to restart.</summary>
        public void Restart() { lastSize = Vector2.zero; }

        /// <summary>Re-fits only when the screen size changes; no allocations.</summary>
        public void Tick(float deltaTime)
        {
            if (image == null) return;
            Vector2 size = image.rectTransform.rect.size;
            if (size == lastSize) return;
            lastSize = size; BrandArt.Cover(image);
            if (arrows != null) { arrows.uvRect = image.uvRect; }
            if (Warp != null) Warp.SetVerticesDirty();
        }

        private void LateUpdate() { if (Application.isPlaying) Tick(0f); }
    }
}
