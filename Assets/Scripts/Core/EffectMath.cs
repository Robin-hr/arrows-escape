using System;

namespace ArrowEscape.Core
{
    /// <summary>Shared pure timing and geometry for sprite effects.</summary>
    public static class EffectMath
    {
        private static float Clamp(float t) => float.IsNaN(t) ? 0 : Math.Max(0,Math.Min(1,t));
        /// <summary>Clamped elapsed fraction; zero-length effects finish immediately.</summary>
        public static float Progress(float elapsed, float duration) => duration <= 0 ? 1 : Clamp(elapsed / duration);
        /// <summary>Travel slows toward the final distance.</summary>
        public static float ParticleDistance(float totalDistance, float t) => totalDistance * AnimationMath.EaseOutCubic(t);
        /// <summary>Quadratic fade to zero.</summary>
        public static float ParticleAlpha(float t) { float remaining = 1 - Clamp(t); return remaining * remaining; }
        /// <summary>Linear size interpolation.</summary>
        public static float ParticleSize(float start, float end, float t) => start + (end - start) * Clamp(t);
        /// <summary>Exactly the distance used by the arrow's slide animation.</summary>
        public static float StreakHeadDistance(int width, int height, int x, int y, Direction direction, float t) =>
            AnimationMath.SlideDistance(width,height,x,y,direction) * AnimationMath.EaseInQuad(t);
        /// <summary>Limits the tail to a maximum distance behind the head.</summary>
        public static float StreakTailDistance(float headDistance, float maxLength)
        { if (!(maxLength >= 0)) throw new ArgumentOutOfRangeException(nameof(maxLength)); return Math.Max(0,headDistance - maxLength); }
        /// <summary>Fades the streak using the existing slide-alpha curve.</summary>
        public static float StreakAlpha(float t, float peakAlpha, float fadeStart) => peakAlpha * AnimationMath.SlideAlpha(t,fadeStart);
        /// <summary>Radial travel time from the origin cell.</summary>
        public static float DotWaveDelay(int x, int y, int originX, int originY, float secondsPerCell)
        { double dx = (double)x - originX, dy = (double)y - originY; return (float)Math.Sqrt(dx * dx + dy * dy) * secondsPerCell; }
        /// <summary>Local pulse progress after the wave reaches this cell.</summary>
        public static float DotWaveLocalTime(float elapsed, float delay, float duration) => elapsed < delay ? 0 : Progress(elapsed - delay,duration);
        /// <summary>Last corner's delay plus the local pulse duration.</summary>
        public static float DotWaveTotalSeconds(int width, int height, int x, int y, float secondsPerCell, float duration) =>
            Math.Max(Math.Max(DotWaveDelay(0,0,x,y,secondsPerCell),DotWaveDelay(width-1,0,x,y,secondsPerCell)),
                Math.Max(DotWaveDelay(0,height-1,x,y,secondsPerCell),DotWaveDelay(width-1,height-1,x,y,secondsPerCell))) + duration;
        /// <summary>A single nonnegative pulse with exact zero endpoints.</summary>
        public static float PulseAmount(float t) => AnimationMath.PulseScale(t,2) - 1;
    }
}
