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
        /// <summary>The warm accent for a press inside the cancellation radius.</summary>
        public static Color HighlightColor => new Color(1f, 0.72f, 0.25f);
        /// <summary>The dimmed accent while a press is dragged away.</summary>
        public static Color DraggedAwayColor => new Color(0.58f, 0.42f, 0.22f);
        /// <summary>Instant scale multiplier while highlighted, without changing position.</summary>
        public const float HighlightScaleMultiplier = 1.12f;
        /// <summary>Seconds for an exited arrow to slide away.</summary>
        public const float SlideDuration = 0.35f;
        /// <summary>Seconds for a blocked arrow to bump and return.</summary>
        public const float BumpDuration = 0.25f;
        /// <summary>Forward bump distance in cells.</summary>
        public const float BumpAmplitude = 0.18f;
        /// <summary>Seconds for the blocker's flash and pulse.</summary>
        public const float FlashDuration = 0.45f;
        /// <summary>Soft red feedback for the blocked arrow.</summary>
        public static Color BlockedTint => new Color(1f, 0.48f, 0.46f);
        /// <summary>Warm amber feedback identifying the nearest blocker.</summary>
        public static Color BlockerTint => new Color(1f, 0.75f, 0.28f);
        /// <summary>Largest multiplier during the blocker pulse.</summary>
        public const float BlockerPulsePeak = 1.15f;
        /// <summary>Sliding arrows render above normal arrows.</summary>
        public const int SlidingSortingOrder = 3;
        /// <summary>Teal suggestion tint, separate from amber blocker feedback.</summary>
        public static Color HintTint => new Color(0.25f, 0.95f, 0.75f);
        /// <summary>Maximum hint scale multiplier.</summary>
        public const float HintPulsePeak = 1.18f;
        /// <summary>Seconds per hint pulse.</summary>
        public const float HintPulsePeriod = 0.8f;
        /// <summary>Number of pulses per request.</summary>
        public const int HintPulseCount = 3;
        /// <summary>Amber blocker connector tint.</summary>
        public static Color GuideLineColor => BlockerTint;
        /// <summary>Connector width in cells.</summary>
        public const float GuideLineThickness = 0.08f;
        /// <summary>End marker size in cells.</summary>
        public const float GuideMarkerSize = 0.30f;
        /// <summary>Inset from each arrow center.</summary>
        public const float GuideInset = 0.35f;
        /// <summary>Seconds a connector remains visible.</summary>
        public const float GuideLineDuration = 1.0f;
        /// <summary>Fraction of the duration before fading starts.</summary>
        public const float GuideFadeStart = 0.7f;
        /// <summary>Connectors render above normal arrows and below slides.</summary>
        public const int GuideSortingOrder = 2;
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
        public const float BottomMargin = 0.16f;
        /// <summary>Largest supported margin fraction, including safe-area insets.</summary>
        public const float MaxMargin = 0.45f;
        /// <summary>Total extra board span per axis, split equally between edges.</summary>
        public const float PaddingCells = 0.5f;
        /// <summary>The camera sits behind the board and looks along positive z.</summary>
        public const float CameraZ = -10f;
        /// <summary>HUD lettering on the dark board background.</summary>
        public static Color HudText => ArrowColor;
        /// <summary>Normal button background.</summary>
        public static Color ButtonNormal => new Color(0.16f, 0.25f, 0.34f);
        /// <summary>Pressed button background.</summary>
        public static Color ButtonPressed => new Color(0.28f, 0.39f, 0.48f);
        /// <summary>Disabled button background.</summary>
        public static Color ButtonDisabled => new Color(0.10f, 0.13f, 0.17f);
        /// <summary>Modal dimming overlay.</summary>
        public static Color PanelDim => new Color(0, 0, 0, 0.8f);
        /// <summary>Completion card background.</summary>
        public static Color PanelCard => new Color(0.08f, 0.13f, 0.20f);
        /// <summary>Completion accent.</summary>
        public static Color UiAccent => HighlightColor;
        /// <summary>HUD font at the 1080 by 1920 reference resolution.</summary>
        public const int HudFontSize = 40;
        /// <summary>Reference button width.</summary>
        public const float ButtonWidth = 300;
        /// <summary>Reference button height.</summary>
        public const float ButtonHeight = 120;
        /// <summary>Reference spacing between buttons.</summary>
        public const float UiSpacing = 24;
        /// <summary>Extra clickable exclusion around board-facing controls.</summary>
        public const float UiBlockPadding = 8;
    }
}
