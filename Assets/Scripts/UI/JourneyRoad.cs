using ArrowEscape.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowEscape.UI
{
    /// <summary>
    /// The winding Journey road, drawn as one mesh for the visible stretch only: a soft neon glow, a dark rim,
    /// the glass road, a lit lane up to the player's current level and a dashed centre line.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class JourneyRoad : MaskableGraphic
    {
        private const float Step = 0.08f;
        private static readonly Color32 Glow = new Color32(56, 225, 255, 70), GlowEdge = new Color32(56, 225, 255, 0);
        private static readonly Color32 Rim = new Color32(12, 10, 44, 245), Road = new Color32(44, 36, 118, 255), RoadEdge = new Color32(70, 60, 170, 255);
        private static readonly Color32 Lit = new Color32(255, 196, 70, 255), LitEdge = new Color32(255, 120, 200, 255);
        private static readonly Color32 Dash = new Color32(235, 240, 255, 150), DashLit = new Color32(255, 255, 240, 230);
        private float from, to, lit = -1;
        private int count;

        /// <summary>Visible start of the road (level index, fractional).</summary>
        public float From => from;
        /// <summary>Visible end of the road (level index, fractional).</summary>
        public float To => to;

        /// <summary>Sets the drawn stretch and the lit progress (current level index); rebuilds only on change.</summary>
        public void Set(int levels, float start, float end, float litTo)
        {
            if (levels == count && Mathf.Approximately(start, from) && Mathf.Approximately(end, to) && Mathf.Approximately(litTo, lit)) return;
            count = levels; from = start; to = end; lit = litTo; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (count <= 0) return;
            float a = Mathf.Max(-0.7f, from), b = Mathf.Min(count - 0.3f, to);
            if (b <= a) return;
            Strip(vh, a, b, 130, GlowEdge, Glow);
            Strip(vh, a, b, 74, Rim, Rim);
            Strip(vh, a, b, 58, RoadEdge, Road);
            if (lit > a) Strip(vh, a, Mathf.Min(b, lit), 40, LitEdge, Lit);
            Dashes(vh, a, b);
        }

        private static Vector2 Point(float t) => new Vector2(JourneyLayout.X(t), JourneyLayout.Y(t));

        private static Vector2 Normal(float t)
        {
            Vector2 d = Point(t + 0.01f) - Point(t - 0.01f);
            return new Vector2(-d.y, d.x).normalized;
        }

        // Ribbon along the road; the edge colour fades to the centre colour.
        private void Strip(VertexHelper vh, float a, float b, float width, Color32 edge, Color32 centre)
        {
            int start = vh.currentVertCount, n = Mathf.CeilToInt((b - a) / Step);
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(a, b, i / (float)n);
                Vector2 p = Point(t), q = Normal(t) * (width * 0.5f);
                vh.AddVert(p - q, edge, Vector4.zero); vh.AddVert(p, centre, Vector4.zero); vh.AddVert(p + q, edge, Vector4.zero);
                if (i == 0) continue;
                int k = start + (i - 1) * 3;
                vh.AddTriangle(k, k + 3, k + 4); vh.AddTriangle(k, k + 4, k + 1);
                vh.AddTriangle(k + 1, k + 4, k + 5); vh.AddTriangle(k + 1, k + 5, k + 2);
            }
        }

        private void Dashes(VertexHelper vh, float a, float b)
        {
            const float period = 0.25f, on = 0.12f;
            float t = Mathf.Ceil(a / period) * period;
            for (; t + on <= b; t += period)
            {
                Color32 c = t < lit ? DashLit : Dash;
                Vector2 p0 = Point(t), p1 = Point(t + on), n0 = Normal(t) * 4f, n1 = Normal(t + on) * 4f;
                int k = vh.currentVertCount;
                vh.AddVert(p0 - n0, c, Vector4.zero); vh.AddVert(p0 + n0, c, Vector4.zero); vh.AddVert(p1 + n1, c, Vector4.zero); vh.AddVert(p1 - n1, c, Vector4.zero);
                vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k, k + 2, k + 3);
            }
        }
    }
}
