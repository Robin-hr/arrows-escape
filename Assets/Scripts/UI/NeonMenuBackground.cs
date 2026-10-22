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
        public void Build(RectTransform parent) { image = BrandArt.Backdrop("NeonBackground", parent, "home_background"); }

        /// <summary>The backdrop is static; nothing to restart.</summary>
        public void Restart() { lastSize = Vector2.zero; }

        /// <summary>Re-fits only when the screen size changes; no allocations.</summary>
        public void Tick(float deltaTime)
        {
            if (image == null) return;
            Vector2 size = image.rectTransform.rect.size;
            if (size == lastSize) return;
            lastSize = size; BrandArt.Cover(image);
        }
    }
}
