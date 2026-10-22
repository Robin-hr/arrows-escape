using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Shared colors, sprite geometry, spacing, and camera settings.</summary>
    public static class ViewTheme
    {
        /// <summary>Path shaft width in cells.</summary>
        public const float PathLineWidth=.26f;
        /// <summary>Path head width in cells.</summary>
        public const float PathHeadWidth=.62f;
        /// <summary>Path head length in cells.</summary>
        public const float PathHeadLength=.42f;
        /// <summary>Blue ice tint; the cut-out silhouette also distinguishes it without color.</summary>
        public static Color IceArrowColor=>new Color(.10f,.36f,.65f);
        /// <summary>Base ice-slide duration.</summary>
        public const float IceSlideBaseSeconds=.10f;
        /// <summary>Additional ice-slide time per cell.</summary>
        public const float IceSlidePerCellSeconds=.05f;
        /// <summary>Maximum ice-slide duration.</summary>
        public const float IceSlideMaxSeconds=.40f;
        /// <summary>Stuck warning reuses the blocker amber.</summary>
        public static Color StuckTextColor=>BlockerTint;
        /// <summary>Small ice-chip burst count.</summary>
        public const int IceBurstCount=6;
        /// <summary>Ice-chip cone half-angle.</summary>
        public const float IceBurstCone=40;
        /// <summary>Shortest ice-chip travel.</summary>
        public const float IceBurstMinDistance=.3f;
        /// <summary>Longest ice-chip travel.</summary>
        public const float IceBurstMaxDistance=.7f;
        /// <summary>Shortest ice-chip lifetime.</summary>
        public const float IceBurstMinLifetime=.20f;
        /// <summary>Longest ice-chip lifetime.</summary>
        public const float IceBurstMaxLifetime=.35f;
        /// <summary>Starting ice-chip size.</summary>
        public const float IceBurstStartSize=.12f;
        /// <summary>Ending ice-chip size.</summary>
        public const float IceBurstEndSize=.03f;
        /// <summary>Ice-chip tint.</summary>
        public static Color IceBurstColor=>IceArrowColor;
        /// <summary>The pale paper background.</summary>
        public static Color Background => new Color(232f/255,242f/255,242f/255);
        /// <summary>The subtle cell marker tint.</summary>
        public static Color CellDot => new Color(197f/255,211f/255,211f/255);
        /// <summary>The dark ink arrow tint.</summary>
        public static Color ArrowColor => new Color(28f/255,34f/255,40f/255);
        /// <summary>The warm accent for a press inside the cancellation radius.</summary>
        public static Color HighlightColor => new Color(.65f,.32f,.02f);
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
        public static Color BlockedTint => new Color(.72f,.16f,.15f);
        /// <summary>Warm amber feedback identifying the nearest blocker.</summary>
        public static Color BlockerTint => new Color(.62f,.36f,.02f);
        /// <summary>Largest multiplier during the blocker pulse.</summary>
        public const float BlockerPulsePeak = 1.15f;
        /// <summary>Sliding arrows render above normal arrows.</summary>
        public const int SlidingSortingOrder = 4;
        /// <summary>Teal suggestion tint, separate from amber blocker feedback.</summary>
        public static Color HintTint => new Color(0f,.45f,.32f);
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
        public const int GuideSortingOrder = 3;
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
        /// <summary>High-contrast HUD lettering on the paper background.</summary>
        public static Color HudText => ArrowColor;
        /// <summary>Earned star tint.</summary>
        public static Color StarGold => new Color32(245,183,0,255);
        /// <summary>Unfilled star slots on paper.</summary>
        public static Color StarGrey => new Color(.48f,.53f,.55f);
        /// <summary>Move budget warning on paper.</summary>
        public static Color MovesAmber => new Color(.57f,.32f,.02f);
        /// <summary>Exceeded two-star budget.</summary>
        public static Color MovesRed => new Color(.72f,.16f,.13f);
        /// <summary>Normal button background.</summary>
        public static Color ButtonNormal => new Color(.70f,.80f,.80f);
        /// <summary>Pressed button background.</summary>
        public static Color ButtonPressed => new Color(.55f,.65f,.65f);
        /// <summary>Disabled button background.</summary>
        public static Color ButtonDisabled => new Color(.80f,.85f,.85f);
        /// <summary>Modal dimming overlay.</summary>
        public static Color PanelDim => new Color(0.01f, 0.01f, 0.07f, 0.84f);
        /// <summary>Completion card background.</summary>
        public static Color PanelCard => new Color(.94f,.97f,.97f);
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

        // Exit streak: behind guides and slides, above normal arrows.
        /// <summary>Sorting order shared by streaks and particles.</summary>
        public const int EffectsSortingOrder = 2;
        /// <summary>Streak width in cells.</summary>
        public const float StreakThickness = .12f;
        /// <summary>Maximum streak length.</summary>
        public const float StreakMaxLength = 2.5f;
        /// <summary>Maximum streak opacity.</summary>
        public const float StreakPeakAlpha = .35f;
        /// <summary>Slide fraction before the streak starts fading.</summary>
        public const float StreakFadeStart = .5f;
        /// <summary>Maximum simultaneous streaks.</summary>
        public const int StreakCapacity = 8;
        /// <summary>Streak tint matches normal arrows.</summary>
        public static Color StreakColor => ArrowColor;
        // Warm exit dots, directed along the arrow.
        /// <summary>Dots emitted per exit.</summary>
        public const int ExitBurstCount = 10;
        /// <summary>Exit burst cone half-angle in degrees.</summary>
        public const float ExitBurstCone = 55;
        /// <summary>Shortest exit particle travel.</summary>
        public const float ExitBurstMinDistance = .6f;
        /// <summary>Longest exit particle travel.</summary>
        public const float ExitBurstMaxDistance = 1.3f;
        /// <summary>Shortest exit particle lifetime.</summary>
        public const float ExitBurstMinLifetime = .3f;
        /// <summary>Longest exit particle lifetime.</summary>
        public const float ExitBurstMaxLifetime = .5f;
        /// <summary>Starting exit dot size.</summary>
        public const float ExitBurstStartSize = .16f;
        /// <summary>Ending exit dot size.</summary>
        public const float ExitBurstEndSize = .03f;
        /// <summary>Exit dots reuse the warm press tint.</summary>
        public static Color ExitBurstColor => HighlightColor;
        /// <summary>Shared cap for exit particles and confetti.</summary>
        public const int ParticleCapacity = 96;
        // Full-circle completion confetti.
        /// <summary>Completion particle count.</summary>
        public const int ConfettiCount = 40;
        /// <summary>Minimum travel multiplier for the board radius.</summary>
        public const float ConfettiMinDistance = .6f;
        /// <summary>Maximum travel multiplier for the board radius.</summary>
        public const float ConfettiMaxDistance = 1.1f;
        /// <summary>Minimum confetti lifetime.</summary>
        public const float ConfettiMinLifetime = .7f;
        /// <summary>Maximum confetti lifetime.</summary>
        public const float ConfettiMaxLifetime = 1.1f;
        /// <summary>Starting confetti size.</summary>
        public const float ConfettiStartSize = .22f;
        /// <summary>Ending confetti size.</summary>
        public const float ConfettiEndSize = .05f;
        /// <summary>Read-only five-color celebration palette; copied once by each effect controller.</summary>
        public static System.Collections.Generic.IReadOnlyList<Color> ConfettiColors => System.Array.AsReadOnly(new[] {
            HighlightColor, HintTint, new Color(.45f,.70f,1f),new Color(1f,.48f,.68f),new Color(.72f,.57f,1f)
        });
        // One outward wave and one gentle background pulse per clear.
        /// <summary>Wave delay per cell from the final arrow.</summary>
        public const float DotWaveSecondsPerCell = .07f;
        /// <summary>Duration of each cell's local pulse.</summary>
        public const float DotWaveDuration = .5f;
        /// <summary>Largest cell-dot scale multiplier.</summary>
        public const float DotWavePeakScale = 2.2f;
        /// <summary>Soft teal wave accent.</summary>
        public static Color DotWaveColor => new Color(.55f,.90f,.85f,.8f);
        /// <summary>Seconds for the single background pulse.</summary>
        public const float CameraPulseDuration = .6f;
        /// <summary>Maximum blend toward the lighter background.</summary>
        public const float CameraPulseStrength = .22f;
        /// <summary>A slightly lighter version of the dark background.</summary>
        public static Color CameraPulseColor => Color.white;
    }
}
