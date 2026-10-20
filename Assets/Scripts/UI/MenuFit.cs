using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Fits a fixed-size menu column when its safe-area rectangle changes.</summary>
    public sealed class MenuFit : MonoBehaviour
    {
        private RectTransform content, available;
        private Vector2 contentSize, lastSize = new Vector2(-1, -1);
        /// <summary>Configures the column and immediately calculates its scale.</summary>
        public void Configure(RectTransform rect, RectTransform safe, Vector2 size)
        { content = rect; available = safe; contentSize = size; Refit(); }
        /// <summary>Updates the scale only when layout geometry changes.</summary>
        public void Refit()
        {
            if (content == null || available == null) return;
            Vector2 size = available.rect.size;
            if (size == lastSize || size.x <= 0 || size.y <= 0) return;
            lastSize = size;
            content.localScale = Vector3.one * UiFit.ComputeScale(contentSize.x, contentSize.y, size.x * 0.92f, size.y * 0.92f);
        }
        private void LateUpdate() => Refit();
    }
}
