using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Keeps runtime UI within the device safe area without rebuilding it.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform rect;
        private Rect lastArea;
        private int lastWidth, lastHeight;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastArea != Screen.safeArea) Apply();
        }
        /// <summary>Applies the current safe-area anchors immediately.</summary>
        public void Apply()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastArea = Screen.safeArea;
            if (lastWidth < 1 || lastHeight < 1) return;
            rect.anchorMin = new Vector2(lastArea.xMin / lastWidth, lastArea.yMin / lastHeight);
            rect.anchorMax = new Vector2(lastArea.xMax / lastWidth, lastArea.yMax / lastHeight);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
