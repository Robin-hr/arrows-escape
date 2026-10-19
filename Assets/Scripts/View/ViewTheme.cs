using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Shared colors, sprite geometry, spacing, and camera settings.</summary>
    public static class ViewTheme
    {
        /// <summary>The dark blue-grey background.</summary>
        public static Color Background => new Color(0.035f, 0.055f, 0.08f);
        /// <summary>The subtle cell marker tint.</summary>
        public static Color CellDot => new Color(0.25f, 0.32f, 0.40f, 0.5f);
        /// <summary>The light arrow tint.</summary>
        public static Color ArrowColor => new Color(0.88f, 0.94f, 1f);
        /// <summary>Arrow size as a fraction of a cell.</summary>
        public const float ArrowFillRatio = 0.7f;
        /// <summary>Cell marker size in world units.</summary>
        public const float CellDotSize = 0.08f;
        /// <summary>Cell markers render behind arrows.</summary>
        public const int CellSortingOrder = 0;
        /// <summary>Arrows render in front of cell markers.</summary>
        public const int ArrowSortingOrder = 1;
        /// <summary>Largest board dimension that still draws cell markers.</summary>
        public const int DotBoardLimit = 32;
        /// <summary>Arrow texture resolution and pixels per world unit.</summary>
        public const int ArrowTextureSize = 128;
        /// <summary>Cell marker texture resolution.</summary>
        public const int DotTextureSize = 32;
        /// <summary>Subsamples per pixel axis for smooth edges.</summary>
        public const int SamplesPerAxis = 4;
        /// <summary>Half the arrow shaft width, in texture-normalized units.</summary>
        public const float ShaftRadius = 0.11f;
        /// <summary>The bottom center of the rounded shaft.</summary>
        public const float ShaftBottom = -0.37f;
        /// <summary>The top center of the rounded shaft.</summary>
        public const float ShaftTop = 0.1f;
        /// <summary>The arrow head's half width.</summary>
        public const float HeadHalfWidth = 0.34f;
        /// <summary>The arrow head's base height.</summary>
        public const float HeadBase = 0.08f;
        /// <summary>The arrow head's tip height.</summary>
        public const float HeadTip = 0.48f;
        /// <summary>The soft cell circle's radius in texture-normalized units.</summary>
        public const float DotRadius = 0.42f;
        /// <summary>Default left and right screen margins.</summary>
        public const float SideMargin = 0.05f;
        /// <summary>Default reserved area above the board.</summary>
        public const float TopMargin = 0.15f;
        /// <summary>Default reserved area below the board.</summary>
        public const float BottomMargin = 0.10f;
        /// <summary>Largest supported margin fraction, including safe-area insets.</summary>
        public const float MaxMargin = 0.45f;
        /// <summary>Total extra board span per axis, split equally between edges.</summary>
        public const float PaddingCells = 0.5f;
        /// <summary>The camera sits behind the board and looks along positive z.</summary>
        public const float CameraZ = -10f;
    }
}
