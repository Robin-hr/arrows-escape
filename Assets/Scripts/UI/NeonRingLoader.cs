using UnityEngine;
using UnityEngine.UI;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Clean neon loading ring: a faint track, a spinning comet arc fading from cyan to magenta and a bright head,
    /// with a gently pulsing core. One UI mesh rebuilt per frame without allocations.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NeonRingLoader : MaskableGraphic
    {
        private const int Segments = 96;
        private static readonly Color32 Head = new Color32(120, 240, 255, 255), Tail = new Color32(255, 95, 200, 0), Track = new Color32(150, 140, 255, 40);
        private float time, start;
        /// <summary>Spin speed multiplier.</summary>
        public float Speed { get; set; } = 1.6f;
        /// <summary>Starts the spin from a phase offset (seconds).</summary>
        public void Restart(float phase = 0f) { time = phase; SetVerticesDirty(); }
        /// <summary>Advances the spin (explicit for tests).</summary>
        public void Tick(float dt) { if (!(dt > 0)) return; time += dt; SetVerticesDirty(); }
        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); raycastTarget = false;
            Rect r = rectTransform.rect; float radius = Mathf.Min(r.width, r.height) * 0.42f, thick = radius * 0.11f;
            Vector2 c = r.center;
            Ring(vh, c, radius, thick * 0.55f, 0, Mathf.PI * 2, Track, Track);
            float head = time * Speed * 4.2f, sweep = Mathf.PI * (1.15f + 0.35f * Mathf.Sin(time * Speed * 1.7f));
            Ring(vh, c, radius, thick, head - sweep, head, Tail, Head);
            Vector2 tip = c + new Vector2(Mathf.Cos(head), Mathf.Sin(head)) * radius;
            Disc(vh, tip, thick * 1.1f, new Color32(255, 255, 255, 255), 0.2f);
            Disc(vh, tip, thick * 2.4f, new Color32(120, 240, 255, 70));
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 4f);
            Disc(vh, c, radius * (0.16f + 0.03f * pulse), new Color32(56, 225, 255, (byte)(120 + 80 * pulse)));
            Disc(vh, c, radius * 0.09f, new Color32(255, 255, 255, 230), 0.3f);
        }

        private static void Ring(VertexHelper vh, Vector2 c, float radius, float thick, float from, float to, Color32 a, Color32 b)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt(Segments * (to - from) / (Mathf.PI * 2)));
            int v0 = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps, ang = Mathf.Lerp(from, to, t);
                Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); Color32 col = Color32.Lerp(a, b, t * t);
                vh.AddVert(c + d * (radius - thick), col, Vector2.zero); vh.AddVert(c + d * (radius + thick), col, Vector2.zero);
                if (i > 0) { int v = v0 + i * 2; vh.AddTriangle(v - 2, v - 1, v + 1); vh.AddTriangle(v - 2, v + 1, v); }
            }
        }

        private static void Disc(VertexHelper vh, Vector2 c, float radius, Color32 color, float softness = 0.85f)
        {
            int v0 = vh.currentVertCount; Color32 edge = color; edge.a = 0; Color32 rim = Color32.Lerp(color, edge, softness);
            vh.AddVert(c, color, Vector2.zero);
            for (int i = 0; i <= 24; i++) { float a = i * Mathf.PI * 2 / 24; vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, rim, Vector2.zero); if (i > 0) vh.AddTriangle(v0, v0 + i, v0 + i + 1); }
        }
    }
}
