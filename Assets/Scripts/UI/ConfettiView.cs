using UnityEngine;
using UnityEngine.UI;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Party-popper confetti over the whole screen: two poppers fire paper pieces up from the bottom corners while
    /// more flutters down from the top. One pooled UI mesh, no allocations per frame; idle when nothing is flying.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConfettiView : MaskableGraphic
    {
        /// <summary>Pieces in one celebration.</summary>
        public const int Count = 180;
        private const float Life = 3.6f, Gravity = 1500f;
        private static readonly Color32[] Palette =
        { new Color32(255, 214, 92, 255), new Color32(56, 225, 255, 255), new Color32(255, 95, 175, 255), new Color32(125, 255, 176, 255), new Color32(180, 120, 255, 255), new Color32(255, 140, 60, 255), new Color32(255, 255, 255, 255) };
        private readonly Vector2[] position = new Vector2[Count], velocity = new Vector2[Count], size = new Vector2[Count];
        private readonly float[] angle = new float[Count], spin = new float[Count], flip = new float[Count], flipSpeed = new float[Count], delay = new float[Count];
        private readonly Color32[] tint = new Color32[Count];
        private float time = Life + 1;
        private uint seed = 12345;
        /// <summary>True while pieces are on screen.</summary>
        public bool IsPlaying => time < Life;
        /// <summary>Seconds since the last burst.</summary>
        public float Elapsed => time;

        private float Random01() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed & 0xFFFFFF) / (float)0xFFFFFF; }

        /// <summary>Fires both poppers and the falling shower.</summary>
        public void Burst()
        {
            Rect r = rectTransform.rect; raycastTarget = false;
            for (int i = 0; i < Count; i++)
            {
                int kind = i % 3; // 0 = left popper, 1 = right popper, 2 = shower from the top
                if (kind < 2)
                {
                    float side = kind == 0 ? 1 : -1;
                    position[i] = new Vector2(kind == 0 ? r.xMin + 20 : r.xMax - 20, r.yMin + 40);
                    float a = Mathf.Deg2Rad * (60 + Random01() * 28), speed = 1500 + Random01() * 1300;
                    velocity[i] = new Vector2(Mathf.Cos(a) * side, Mathf.Sin(a)) * speed; delay[i] = Random01() * 0.12f;
                }
                else
                {
                    position[i] = new Vector2(Mathf.Lerp(r.xMin, r.xMax, Random01()), r.yMax + 40 + Random01() * 300);
                    velocity[i] = new Vector2((Random01() - .5f) * 200, -150 - Random01() * 250); delay[i] = 0.25f + Random01() * 0.6f;
                }
                size[i] = new Vector2(16 + Random01() * 14, 24 + Random01() * 18);
                angle[i] = Random01() * 360; spin[i] = (Random01() - .5f) * 720;
                flip[i] = Random01() * 6.28f; flipSpeed[i] = 6 + Random01() * 10;
                tint[i] = Palette[(int)(Random01() * Palette.Length) % Palette.Length];
            }
            time = 0; SetVerticesDirty();
        }

        /// <summary>Advances the simulation (explicit for tests).</summary>
        public void Tick(float dt)
        {
            if (!IsPlaying || !(dt > 0)) return;
            dt = Mathf.Min(dt, 0.05f); time += dt;
            for (int i = 0; i < Count; i++)
            {
                if (time < delay[i]) continue;
                velocity[i].y -= Gravity * dt * (i % 3 == 2 ? 0.35f : 1f);
                velocity[i] *= 1f - Mathf.Min(1, 2.2f * dt);                 // air drag: paper slows and floats
                float sway = Mathf.Sin(time * 3 + flip[i]) * 60;
                position[i] += (velocity[i] + new Vector2(sway, 0)) * dt;
                angle[i] += spin[i] * dt; flip[i] += flipSpeed[i] * dt;
            }
            SetVerticesDirty();
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!IsPlaying) return;
            float fade = Mathf.Clamp01((Life - time) / 0.8f);
            for (int i = 0; i < Count; i++)
            {
                if (time < delay[i]) continue;
                float rad = angle[i] * Mathf.Deg2Rad, c = Mathf.Cos(rad), s = Mathf.Sin(rad);
                float w = size[i].x * 0.5f * Mathf.Abs(Mathf.Cos(flip[i])), h = size[i].y * 0.5f; // paper flipping in 3D
                Vector2 ax = new Vector2(c, s) * w, ay = new Vector2(-s, c) * h, p = position[i];
                Color32 col = tint[i]; float shade = 0.75f + 0.25f * Mathf.Cos(flip[i]);
                col.r = (byte)(col.r * shade); col.g = (byte)(col.g * shade); col.b = (byte)(col.b * shade); col.a = (byte)(255 * fade);
                int v = vh.currentVertCount;
                vh.AddVert(p - ax - ay, col, Vector2.zero); vh.AddVert(p + ax - ay, col, Vector2.zero);
                vh.AddVert(p + ax + ay, col, Vector2.zero); vh.AddVert(p - ax + ay, col, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
