using System;

namespace ArrowEscape.Core
{
    /// <summary>Clamped animation curves and slide distances with no rendering dependencies.</summary>
    public static class AnimationMath
    {
        /// <summary>Path travel at 28 cells per second, bounded for readable feedback.</summary>
        public static float PathExitDuration(float trackCells) => System.Math.Max(.18f,System.Math.Min(.90f,trackCells/28f));
        /// <summary>Clamps ice-slide time; nonpositive cell counts use the base duration.</summary>
        public static float IceSlideDuration(int cells,float baseSeconds,float perCellSeconds,float maxSeconds)
        {
            if(float.IsNaN(baseSeconds)||float.IsInfinity(baseSeconds)||baseSeconds<0)throw new ArgumentOutOfRangeException(nameof(baseSeconds));
            if(float.IsNaN(perCellSeconds)||float.IsInfinity(perCellSeconds)||perCellSeconds<0)throw new ArgumentOutOfRangeException(nameof(perCellSeconds));
            if(float.IsNaN(maxSeconds)||float.IsInfinity(maxSeconds)||maxSeconds<baseSeconds)throw new ArgumentOutOfRangeException(nameof(maxSeconds));
            return (float)Math.Min(maxSeconds,baseSeconds+(double)Math.Max(0,cells)*perCellSeconds);
        }
        /// <summary>Remaining slide offset, with exact one/zero endpoints.</summary>
        public static float SlideOffsetFactor(float t)=>1-EaseOutCubic(t);
        /// <summary>Accelerates quadratically from zero to one.</summary>
        public static float EaseInQuad(float t)
        {
            t = Clamp01(t);
            return t * t;
        }

        /// <summary>Decelerates cubically from zero to one.</summary>
        public static float EaseOutCubic(float t)
        {
            float remaining = 1 - Clamp01(t);
            return 1 - remaining * remaining * remaining;
        }

        /// <summary>Accelerates then decelerates quadratically from zero to one.</summary>
        public static float EaseInOutQuad(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
        }

        /// <summary>Returns distance to the board edge plus an extra span outside it, in cells.</summary>
        public static float SlideDistance(int boardWidth, int boardHeight, int x, int y,
            Direction direction, float extraCells = 1.5f)
        {
            switch (direction)
            {
                case Direction.Up: return (boardHeight - 1 - y) + extraCells;
                case Direction.Down: return y + extraCells;
                case Direction.Right: return (boardWidth - 1 - x) + extraCells;
                case Direction.Left: return x + extraCells;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>Stays opaque until fadeStart, then smoothly fades to exactly zero.</summary>
        public static float SlideAlpha(float t, float fadeStart = 0.7f)
        {
            t = Clamp01(t);
            fadeStart = Clamp01(fadeStart);
            if (t >= 1) return 0;
            if (t <= fadeStart) return 1;
            return 1 - EaseInOutQuad((t - fadeStart) / (1 - fadeStart));
        }

        /// <summary>Bumps forward to the nonnegative amplitude at t=0.35, then returns exactly to zero.</summary>
        public static float BumpOffset(float t, float amplitude) => Math.Max(0, amplitude) * Pulse(t, 0.35f);

        /// <summary>Pulses from one to peak at t=0.4, then returns exactly to one.</summary>
        public static float PulseScale(float t, float peak) => 1 + (peak - 1) * Pulse(t, 0.4f);

        private static float Pulse(float t, float peakTime)
        {
            t = Clamp01(t);
            return t <= peakTime ? EaseOutCubic(t / peakTime) :
                1 - EaseInOutQuad((t - peakTime) / (1 - peakTime));
        }

        private static float Clamp01(float t) => float.IsNaN(t) ? 0 : Math.Max(0, Math.Min(1, t));
    }
}
