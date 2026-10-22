using ArrowEscape.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Neon snake-arrow loading element: a 7x7 puzzle tile that solves itself. One mesh, no material or texture,
    /// works at any size (startup, level start, profile). Driven by unscaled time so pause never freezes it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SnakeLoaderGraphic : MaskableGraphic
    {
        private const float ViewUnits = 7.6f, Corner = 0.9f, LineWidth = 0.24f, GlowWidth = 0.62f, CoreWidth = 0.08f;
        private static readonly Color32[] Palette =
        {
            new Color32(63, 196, 255, 255),  // cyan
            new Color32(255, 184, 31, 255),  // yellow
            new Color32(82, 224, 90, 255),   // green
            new Color32(255, 79, 139, 255),  // pink
            new Color32(255, 255, 255, 255), // blocker flash
        };
        private static readonly Color32 TileFill = new Color32(11, 16, 64, 240);
        private static readonly Color32 TileEdge = new Color32(56, 225, 255, 255);
        private static readonly Color32 Dot = new Color32(120, 110, 220, 150);

        private SnakeLoaderModel model;
        private float[] xs, ys, sx, sy;
        private const float BendRadius = 0.38f;
        private const int BendSteps = 5;
        /// <summary>Draw the built-in tile; off when the screen supplies the neon panel behind the puzzle.</summary>
        public bool DrawTile { get; set; } = true;
        private float time;
        private float scale;
        private Vector2 center;

        /// <summary>Animation speed multiplier; short loaders run faster so a solve is visible.</summary>
        public float Speed { get; set; } = 1f;
        /// <summary>Current animation time in seconds.</summary>
        public float Time => time;
        /// <summary>The underlying puzzle model.</summary>
        public SnakeLoaderModel Model { get { Ensure(); return model; } }

        private void Ensure()
        {
            if (model != null) return;
            model = new SnakeLoaderModel(); xs = new float[model.MaxBodyPoints]; ys = new float[model.MaxBodyPoints];
            sx = new float[model.MaxBodyPoints * (BendSteps + 1) + 2]; sy = new float[sx.Length];
            raycastTarget = false;
        }

        /// <summary>Restarts the puzzle from its first move.</summary>
        public void Restart(float startTime = 0f) { Ensure(); time = startTime; SetVerticesDirty(); }

        /// <summary>Advances the animation (scaled by Speed) and rebuilds the mesh.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) return;
            Ensure(); time += dt * Speed; SetVerticesDirty();
        }

        private void Update() { if (Application.isPlaying) Tick(UnityEngine.Time.unscaledDeltaTime); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Ensure();
            Rect r = rectTransform.rect;
            scale = Mathf.Min(r.width, r.height) / ViewUnits; if (scale <= 0) return;
            center = r.center;
            model.Evaluate(time);
            if (DrawTile) DrawTileMesh(vh);
            for (int x = 0; x <= SnakeLoaderModel.Size; x++)
                for (int y = 0; y <= SnakeLoaderModel.Size; y++) Disc(vh, P(x, y), 0.075f * scale, Dot);
            // Three passes so glows sit under every core line.
            for (int pass = 0; pass < 3; pass++)
                for (int i = 0; i < model.SnakeCount; i++) DrawSnake(vh, i, pass);
        }

        private Vector2 P(float x, float y) => new Vector2(center.x + (x - 3f) * scale, center.y - (y - 3f) * scale);

        private void DrawSnake(VertexHelper vh, int i, int pass)
        {
            float op = model.Opacity(i); if (op <= 0.001f) return;
            Color32 baseColor = Palette[model.ColorSlot(i)];
            int n = model.GetBody(i, xs, ys);
            float width = pass == 0 ? GlowWidth : pass == 1 ? LineWidth : CoreWidth;
            float alpha = pass == 0 ? 0.28f : pass == 1 ? 1f : 0.55f;
            Color32 c = pass == 2 ? new Color32(255, 255, 255, 255) : baseColor;
            c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(255 * alpha * op * color.a), 0, 255);
            // Rounded bends like the game's arrows: fillet every corner, then draw clipped segments with round joints.
            int m = Smooth(n);
            float half = width * 0.5f;
            for (int k = 1; k < m; k++) Line(vh, sx[k - 1], sy[k - 1], sx[k], sy[k], half, c);
            if (pass == 1) for (int k = 1; k < m - 1; k++) if (Inside(sx[k], sy[k])) Disc(vh, P(sx[k], sy[k]), half * scale, c);
            if (pass == 2) return;
            // Head: a triangle ahead of the last point, hidden once it leaves the tile.
            model.Direction(i, out float dx, out float dy);
            float hx = xs[n - 1], hy = ys[n - 1];
            float tipX = hx + dx * 0.38f, tipY = hy + dy * 0.38f;
            if (tipX < SnakeLoaderModel.TileMin || tipX > SnakeLoaderModel.TileMax || tipY < SnakeLoaderModel.TileMin || tipY > SnakeLoaderModel.TileMax) return;
            float grow = pass == 0 ? 1.7f : 1f, px = -dy, py = dx;
            float bx = hx - dx * (grow - 1) * 0.2f, by = hy - dy * (grow - 1) * 0.2f;
            Triangle(vh, P(hx + dx * 0.38f * grow, hy + dy * 0.38f * grow), P(bx + px * 0.3f * grow, by + py * 0.3f * grow), P(bx - px * 0.3f * grow, by - py * 0.3f * grow), c);
        }

        // Fillets each interior corner of the body (xs, ys) into sx, sy; returns the point count.
        private int Smooth(int n)
        {
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                if (i > 0 && i < n - 1)
                {
                    float ax = xs[i] - xs[i - 1], ay = ys[i] - ys[i - 1], bx = xs[i + 1] - xs[i], by = ys[i + 1] - ys[i];
                    float la = Mathf.Sqrt(ax * ax + ay * ay), lb = Mathf.Sqrt(bx * bx + by * by);
                    if (la > 1e-4f && lb > 1e-4f && Mathf.Abs((ax * bx + ay * by) / (la * lb)) < 0.999f)
                    {
                        float r = Mathf.Min(BendRadius, la * 0.5f, lb * 0.5f);
                        float s0x = xs[i] - ax / la * r, s0y = ys[i] - ay / la * r, e0x = xs[i] + bx / lb * r, e0y = ys[i] + by / lb * r;
                        for (int t = 0; t <= BendSteps; t++)
                        {
                            float u = (float)t / BendSteps, v = 1 - u;
                            sx[m] = v * v * s0x + 2 * v * u * xs[i] + u * u * e0x; sy[m] = v * v * s0y + 2 * v * u * ys[i] + u * u * e0y; m++;
                        }
                        continue;
                    }
                }
                sx[m] = xs[i]; sy[m] = ys[i]; m++;
            }
            return m;
        }

        private static bool Inside(float x, float y)
        {
            float lo = SnakeLoaderModel.TileMin + 0.05f, hi = SnakeLoaderModel.TileMax - 0.05f;
            return x >= lo && x <= hi && y >= lo && y <= hi;
        }

        // Any-direction segment clipped to the tile (Liang-Barsky), drawn as a quad.
        private void Line(VertexHelper vh, float x0, float y0, float x1, float y1, float half, Color32 c)
        {
            float lo = SnakeLoaderModel.TileMin + 0.05f, hi = SnakeLoaderModel.TileMax - 0.05f;
            float dx = x1 - x0, dy = y1 - y0, t0 = 0, t1 = 1;
            if (!Clip(-dx, x0 - lo, ref t0, ref t1) || !Clip(dx, hi - x0, ref t0, ref t1) || !Clip(-dy, y0 - lo, ref t0, ref t1) || !Clip(dy, hi - y0, ref t0, ref t1)) return;
            float ax = x0 + dx * t0, ay = y0 + dy * t0, bx = x0 + dx * t1, by = y0 + dy * t1;
            float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)); if (len < 1e-4f) return;
            float nx = -(by - ay) / len * half, ny = (bx - ax) / len * half;
            int i = vh.currentVertCount;
            vh.AddVert(P(ax + nx, ay + ny), c, Vector2.zero); vh.AddVert(P(bx + nx, by + ny), c, Vector2.zero);
            vh.AddVert(P(bx - nx, by - ny), c, Vector2.zero); vh.AddVert(P(ax - nx, ay - ny), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-6f) return q >= 0;
            float r = q / p;
            if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
            else { if (r < t0) return false; if (r < t1) t1 = r; }
            return true;
        }

        private void DrawTileMesh(VertexHelper vh)
        {
            Color32 glow = TileEdge; glow.a = (byte)(70 * color.a);
            Color32 fill = TileFill; fill.a = (byte)(fill.a * color.a);
            Color32 edge = TileEdge; edge.a = (byte)(255 * color.a);
            RoundedRect(vh, SnakeLoaderModel.TileMin - 0.18f, SnakeLoaderModel.TileMax + 0.18f, Corner + 0.18f, glow);
            RoundedRect(vh, SnakeLoaderModel.TileMin - 0.07f, SnakeLoaderModel.TileMax + 0.07f, Corner + 0.07f, edge);
            RoundedRect(vh, SnakeLoaderModel.TileMin, SnakeLoaderModel.TileMax, Corner, fill);
        }

        private void RoundedRect(VertexHelper vh, float min, float max, float radius, Color32 c)
        {
            int start = vh.currentVertCount;
            vh.AddVert(P((min + max) * 0.5f, (min + max) * 0.5f), c, Vector2.zero);
            const int steps = 6;
            for (int corner = 0; corner < 4; corner++)
            {
                // Corners in angle order: (max,min) (max,max) (min,max) (min,min).
                float cx = corner < 2 ? max - radius : min + radius;
                float cy = corner == 0 || corner == 3 ? min + radius : max - radius;
                float a0 = corner * 90f - 90f;
                for (int s = 0; s <= steps; s++)
                {
                    float a = (a0 + 90f * s / steps) * Mathf.Deg2Rad;
                    vh.AddVert(P(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius), c, Vector2.zero);
                }
            }
            int ring = 4 * (steps + 1);
            for (int k = 0; k < ring; k++) vh.AddTriangle(start, start + 1 + k, start + 1 + (k + 1) % ring);
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(a.x, a.y), c, Vector2.zero); vh.AddVert(new Vector3(a.x, b.y), c, Vector2.zero);
            vh.AddVert(new Vector3(b.x, b.y), c, Vector2.zero); vh.AddVert(new Vector3(b.x, a.y), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        private static void Disc(VertexHelper vh, Vector2 p, float radius, Color32 c)
        {
            int i = vh.currentVertCount; const int sides = 8;
            vh.AddVert(p, c, Vector2.zero);
            for (int s = 0; s < sides; s++)
            {
                float a = s * Mathf.PI * 2f / sides;
                vh.AddVert(new Vector3(p.x + Mathf.Cos(a) * radius, p.y + Mathf.Sin(a) * radius), c, Vector2.zero);
            }
            for (int s = 0; s < sides; s++) vh.AddTriangle(i, i + 1 + s, i + 1 + (s + 1) % sides);
        }
    }
}
