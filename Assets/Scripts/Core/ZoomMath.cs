using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure camera zoom and drag calculations in world and screen units.</summary>
    public static class ZoomMath
    {
        /// <summary>Clamps orthographic size between detailed view and the original fit.</summary>
        public static float ClampSize(float size,float fitSize,float pixelHeight,float dpi)
        {
            if(!(dpi>0))dpi=160;
            float minimum=Math.Min(fitSize,pixelHeight/(2*.45f*dpi));
            return Math.Max(minimum,Math.Min(fitSize,size));
        }
        /// <summary>Camera center preserving a world pivot while changing size.</summary>
        public static float PivotCenter(float center,float worldPivot,float oldSize,float newSize)
            => worldPivot+(center-worldPivot)*newSize/oldSize;
        /// <summary>Limits panning so the viewport remains over the board, or contains it when it fits.</summary>
        public static float ClampCenter(float center,float boardHalfSize,float visibleHalfSize)
        {
            float range=Math.Abs(boardHalfSize-visibleHalfSize);
            return Math.Max(-range,Math.Min(range,center));
        }
        /// <summary>A movement beyond twelve screen pixels cancels a path-board tap.</summary>
        public static bool CancelsTap(float dx,float dy)=>dx*dx+dy*dy>144;
    }
}
