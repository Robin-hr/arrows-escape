using UnityEngine;
using UnityEngine.UI;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Animated layer of the neon backdrop: light streaks fly out of the vanishing point (warp speed) and the floor grid
    /// scrolls toward the viewer. Both use one camera model and one speed, so they move in step. Drawn in the art's
    /// own pixel space (1080 x 1920) and mapped through the backdrop's cover-fit UVs so it always lines up with it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WarpBackground : MaskableGraphic
    {
        private const float ArtW = 1080f, ArtH = 1920f;
        private const float HorizonY = ArtH * 0.30f;            // art y-up
        private static readonly Vector2 Vanish = new Vector2(ArtW * 0.5f, ArtH * 0.67f);
        /// <summary>Forward speed in depth units per second, shared by streaks and grid.</summary>
        public const float Speed = 1.0f;
        private const int Streaks = 140;
        private const float Far = 6f, Near = 0.35f, Focal = 420f, Tail = 0.75f;
        private const int GridRows = 40, GridColumns = 14;
        private const float GridSpacing = 120f;
        private static readonly Color32[] StreakColors =
        { new Color32(90, 216, 255, 255), new Color32(156, 134, 255, 255), new Color32(232, 238, 255, 255), new Color32(70, 180, 255, 255) };
        private static readonly Color32 RowColor = new Color32(164, 92, 255, 255), ColumnColor = new Color32(138, 76, 255, 255);

        private RawImage reference;
        private float time;

        /// <summary>Clock driving the animation (seconds); public for tests and captures.</summary>
        public float Clock => time;

        /// <summary>Follows the cover-fit of the static backdrop image.</summary>
        public void Bind(RawImage backdrop) { reference = backdrop; raycastTarget = false; SetVerticesDirty(); }

        /// <summary>Advances the shared clock (unscaled, so pause menus keep moving).</summary>
        public void Tick(float dt) { if (!(dt > 0) || float.IsInfinity(dt)) return; time += Mathf.Min(dt, 0.1f); SetVerticesDirty(); }

        private static float sharedClock;
        private static int sharedFrame = -1;
        // Every warp layer reads one shared clock, so switching screens never makes the streaks or grid jump.
        private void Update()
        {
            if (!Application.isPlaying) return;
            if (Time.frameCount != sharedFrame) { sharedFrame = Time.frameCount; sharedClock += Mathf.Min(Time.unscaledDeltaTime, 0.1f); }
            if (time != sharedClock) { time = sharedClock; SetVerticesDirty(); }
        }

        private static float Hash(int a, int b)
        {
            unchecked
            {
                uint x = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                x = (x ^ (x >> 13)) * 1274126177u;
                return ((x ^ (x >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect; if (r.width <= 0 || r.height <= 0) return;
            Rect uv = reference != null ? reference.uvRect : new Rect(0, 0, 1, 1);
            float sx = r.width / (uv.width * ArtW), sy = r.height / (uv.height * ArtH);
            Vector2 offset = new Vector2(r.xMin - uv.xMin * ArtW * sx, r.yMin - uv.yMin * ArtH * sy);
            float pixel = Mathf.Min(sx, sy);
            float span = Far - Near, alpha = color.a;

            // Warp streaks: each one travels from far to near along a fixed ray, then respawns on a new ray.
            for (int i = 0; i < Streaks; i++)
            {
                float travel = time * Speed + Hash(i, 7) * span;
                int cycle = Mathf.FloorToInt(travel / span);
                float z = Far - (travel - cycle * span);
                float angle = Hash(i, cycle + 11) * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 a = Vanish + dir * (Focal / (z + Tail)), b = Vanish + dir * (Focal / z);
                if (!ClipAboveHorizon(ref a, ref b)) continue;
                float strength = Mathf.Clamp01((Far - z) / 1.5f) * 0.75f * alpha;
                Color32 head = StreakColors[i & 3]; head.a = (byte)(255 * strength);
                Color32 tail = head; tail.a = 0;
                Line(vh, Map(a, sx, sy, offset), Map(b, sx, sy, offset), Mathf.Min(5f, 1.2f + 2.2f / z) * pixel, tail, head);
            }

            // Floor rows at depths k + 1 - phase scroll toward the viewer at the same speed.
            float phase = time * Speed % 1f;
            for (int k = 0; k < GridRows; k++)
            {
                float z = k + 1 - phase; if (z < 1f) continue;
                float y = HorizonY * (1f - 1f / z);
                Color32 c = RowColor; c.a = (byte)(255 * Mathf.Min(1f, 1.6f / z) * 0.55f * alpha);
                Line(vh, Map(new Vector2(0, y), sx, sy, offset), Map(new Vector2(ArtW, y), sx, sy, offset), (1f + 2.6f / z) * pixel, c, c);
            }
            // Columns converge on the horizon's vanishing point; they do not move when travelling straight ahead.
            for (int x = -GridColumns; x <= GridColumns; x++)
            {
                Color32 bottom = ColumnColor; bottom.a = (byte)(153 * alpha); Color32 top = ColumnColor; top.a = 0;
                Line(vh, Map(new Vector2(ArtW * 0.5f + x * GridSpacing, 0), sx, sy, offset), Map(new Vector2(ArtW * 0.5f, HorizonY), sx, sy, offset), 1.6f * pixel, bottom, top);
            }
        }

        private static Vector2 Map(Vector2 art, float sx, float sy, Vector2 offset) => new Vector2(offset.x + art.x * sx, offset.y + art.y * sy);

        // Streaks belong to the sky; the part below the horizon is cut so the floor stays clean.
        private static bool ClipAboveHorizon(ref Vector2 a, ref Vector2 b)
        {
            if (a.y < HorizonY && b.y < HorizonY) return false;
            if (b.y < HorizonY) { float f = (HorizonY - a.y) / (b.y - a.y); b = a + (b - a) * f; }
            else if (a.y < HorizonY) { float f = (HorizonY - b.y) / (a.y - b.y); a = b + (a - b) * f; }
            return true;
        }

        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 ca, Color32 cb)
        {
            Vector2 d = b - a; float len = d.magnitude; if (len < 0.01f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len * (width * 0.5f);
            int i = vh.currentVertCount;
            vh.AddVert(a + n, ca, Vector2.zero); vh.AddVert(b + n, cb, Vector2.zero);
            vh.AddVert(b - n, cb, Vector2.zero); vh.AddVert(a - n, ca, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
