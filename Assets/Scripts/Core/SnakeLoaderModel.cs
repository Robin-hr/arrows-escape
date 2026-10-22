using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// The loading animation: a small 7x7 snake-arrow puzzle that solves itself, then resets.
    /// Pure, deterministic math in lattice units (x right, y DOWN, 0..6). No allocations after construction.
    /// One snake tries too early, bumps, and its blocker flashes white, exactly like the real game.
    /// </summary>
    public sealed class SnakeLoaderModel
    {
        /// <summary>Largest lattice coordinate (the board is 7 x 7 points).</summary>
        public const int Size = 6;
        /// <summary>Inner edge of the rounded tile; anything beyond is clipped.</summary>
        public const float TileMin = -0.62f;
        /// <summary>Outer edge of the rounded tile.</summary>
        public const float TileMax = 6.62f;
        /// <summary>Color slots: 0 cyan, 1 yellow, 2 green, 3 pink, 4 white flash.</summary>
        public const int FlashColor = 4;

        private const float Gap = 0.42f, Bump = 0.4f, FlashSeconds = 0.6f;

        // Each snake: lattice points from tail to head. The head's last step sets the exit direction.
        private static readonly int[][] Snakes =
        {
            new[] { 5, 4, 6, 4, 6, 3, 6, 2, 6, 1, 6, 0 },
            new[] { 3, 5, 3, 6, 2, 6, 1, 6, 0, 6 },
            new[] { 4, 6, 5, 6, 6, 6 },
            new[] { 0, 2, 0, 3, 0, 4, 0, 5, 1, 5, 2, 5 },
            new[] { 4, 5, 5, 5, 6, 5 },
            new[] { 1, 4, 2, 4, 3, 4, 4, 4 },
            new[] { 1, 3, 2, 3, 2, 2, 3, 2, 4, 2, 5, 2 },
            new[] { 3, 3, 4, 3, 5, 3 },
            new[] { 1, 2, 1, 1, 2, 1, 3, 1 },
            new[] { 2, 0, 3, 0, 4, 0, 4, 1, 5, 1 },
            new[] { 0, 1, 0, 0, 1, 0 },
        };
        // Neighbouring snakes never share a color.
        private static readonly int[] Colors = { 1, 0, 3, 1, 2, 3, 0, 2, 2, 3, 0 };
        // Valid solving order: exit (e) or bump-and-flash (b, mover, blocker).
        private static readonly int[][] Events =
        {
            new[] { 0, 0 }, new[] { 0, 4 }, new[] { 1, 8, 9 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 5 },
            new[] { 0, 7 }, new[] { 0, 6 }, new[] { 0, 9 }, new[] { 0, 3 }, new[] { 0, 8 }, new[] { 0, 10 },
        };

        private readonly int[] segments;
        private readonly float[] dirX, dirY, travel, duration, startTime, bumpTime, flashTime;
        private readonly float[][] pathX, pathY, cumulative;
        private readonly float[] offset, opacity;
        private readonly int[] color;

        /// <summary>Number of snakes in the puzzle.</summary>
        public int SnakeCount => Snakes.Length;
        /// <summary>Seconds for one full solve plus the reset pause.</summary>
        public float Period { get; }
        /// <summary>Largest number of points any body can need.</summary>
        public int MaxBodyPoints { get; }

        /// <summary>Precomputes every path once.</summary>
        public SnakeLoaderModel()
        {
            int count = Snakes.Length;
            segments = new int[count]; dirX = new float[count]; dirY = new float[count]; travel = new float[count];
            duration = new float[count]; startTime = new float[count]; bumpTime = new float[count]; flashTime = new float[count];
            pathX = new float[count][]; pathY = new float[count][]; cumulative = new float[count][];
            offset = new float[count]; opacity = new float[count]; color = new int[count];
            int maxPoints = 0;
            for (int i = 0; i < count; i++)
            {
                int[] p = Snakes[i]; int n = p.Length / 2 - 1; segments[i] = n;
                int hx = p[2 * n], hy = p[2 * n + 1];
                int dx = hx - p[2 * n - 2], dy = hy - p[2 * n - 1];
                dirX[i] = dx; dirY[i] = dy;
                int edge = dx > 0 ? Size - hx : dx < 0 ? hx : dy > 0 ? Size - hy : hy;
                float e = edge + n + 0.8f; travel[i] = e;
                pathX[i] = new float[n + 2]; pathY[i] = new float[n + 2]; cumulative[i] = new float[n + 2];
                for (int k = 0; k <= n; k++) { pathX[i][k] = p[2 * k]; pathY[i][k] = p[2 * k + 1]; }
                pathX[i][n + 1] = hx + dx * e; pathY[i][n + 1] = hy + dy * e;
                for (int k = 1; k < n + 2; k++)
                {
                    float ddx = pathX[i][k] - pathX[i][k - 1], ddy = pathY[i][k] - pathY[i][k - 1];
                    cumulative[i][k] = cumulative[i][k - 1] + (float)Math.Sqrt(ddx * ddx + ddy * ddy);
                }
                duration[i] = 0.35f + e * 0.07f;
                startTime[i] = bumpTime[i] = flashTime[i] = float.NaN;
                maxPoints = Math.Max(maxPoints, n + 4);
            }
            MaxBodyPoints = maxPoints;
            float end = 0;
            for (int k = 0; k < Events.Length; k++)
            {
                float t = 0.5f + k * Gap; int[] ev = Events[k];
                if (ev[0] == 0) startTime[ev[1]] = t;
                else { bumpTime[ev[1]] = t; flashTime[ev[2]] = t; }
            }
            for (int i = 0; i < count; i++) if (!float.IsNaN(startTime[i])) end = Math.Max(end, startTime[i] + duration[i]);
            Period = end + 1.8f;
        }

        /// <summary>Moves every snake to its pose at the given time (wraps around the period).</summary>
        public void Evaluate(float time)
        {
            float t = time % Period; if (t < 0) t += Period;
            float reset = t - (Period - 0.8f);
            for (int i = 0; i < offset.Length; i++)
            {
                float s = 0, op = 1; int c = Colors[i];
                if (!float.IsNaN(startTime[i]))
                {
                    float local = t - startTime[i];
                    if (local >= 0 && local < duration[i])
                    {
                        float p = local / duration[i]; s = travel[i] * p * p; op = p < 0.7f ? 1 : 1 - (p - 0.7f) / 0.3f;
                    }
                    else if (local >= duration[i]) op = reset > 0 ? reset / 0.8f : 0;
                }
                if (!float.IsNaN(bumpTime[i]))
                {
                    float local = t - bumpTime[i];
                    if (local >= 0 && local < Bump) s = 0.35f * (float)Math.Sin(Math.PI * local / Bump);
                }
                if (!float.IsNaN(flashTime[i]))
                {
                    float local = t - flashTime[i];
                    if (local >= 0 && local < FlashSeconds) c = FlashColor;
                }
                offset[i] = s; opacity[i] = Math.Max(0, Math.Min(1, op)); color[i] = c;
            }
        }

        /// <summary>Opacity 0..1 of a snake after the last Evaluate.</summary>
        public float Opacity(int snake) => opacity[snake];
        /// <summary>Color slot of a snake after the last Evaluate (see FlashColor).</summary>
        public int ColorSlot(int snake) => color[snake];
        /// <summary>Exit direction of a snake in lattice units (y down).</summary>
        public void Direction(int snake, out float x, out float y) { x = dirX[snake]; y = dirY[snake]; }

        /// <summary>How many snakes have started leaving by this time (for a progress caption).</summary>
        public int StartedCount(float time)
        {
            float t = time % Period; if (t < 0) t += Period; int n = 0;
            for (int i = 0; i < startTime.Length; i++) if (!float.IsNaN(startTime[i]) && t >= startTime[i]) n++;
            return n;
        }

        /// <summary>Writes the visible body (tail to head) into the buffers and returns the point count.</summary>
        public int GetBody(int snake, float[] xs, float[] ys)
        {
            float a = Math.Max(0, offset[snake]), b = offset[snake] + segments[snake];
            int count = 0;
            At(snake, a, out xs[count], out ys[count]); count++;
            float[] cum = cumulative[snake];
            for (int k = 0; k < cum.Length; k++)
                if (cum[k] > a && cum[k] < b) { xs[count] = pathX[snake][k]; ys[count] = pathY[snake][k]; count++; }
            At(snake, b, out xs[count], out ys[count]); count++;
            return count;
        }

        private void At(int snake, float d, out float x, out float y)
        {
            float[] cum = cumulative[snake]; int last = cum.Length - 1;
            if (d >= cum[last]) { x = pathX[snake][last]; y = pathY[snake][last]; return; }
            int k = 0; while (k < last - 1 && cum[k + 1] < d) k++;
            float span = cum[k + 1] - cum[k], f = span > 0 ? (d - cum[k]) / span : 0;
            x = pathX[snake][k] + (pathX[snake][k + 1] - pathX[snake][k]) * f;
            y = pathY[snake][k] + (pathY[snake][k + 1] - pathY[snake][k]) * f;
        }
    }
}
