using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Level-start transition: arrows drop into place one after another, in a wave from the shape's centre outward.
    /// Visual only (offset and alpha layers); gameplay input should be blocked while <see cref="IsPlaying"/>.
    /// </summary>
    public sealed class BoardIntro : MonoBehaviour
    {
        /// <summary>Time for one arrow to land.</summary>
        public const float ArrowTime = 0.38f;
        /// <summary>Longest total stagger across the whole board.</summary>
        public const float MaxStagger = 0.45f;
        /// <summary>Starting lift in cells.</summary>
        public const float Lift = 0.7f;
        private readonly List<ArrowView> views = new List<ArrowView>(64);
        private readonly List<float> starts = new List<float>(64);
        private BoardView board;
        private float elapsed, total;
        /// <summary>True from Play until every arrow has landed.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Hides the arrows and lands them after an optional delay (e.g. while a loading screen fades).</summary>
        public void Play(float delay = 0f)
        {
            if (board == null) board = GetComponent<BoardView>();
            views.Clear(); starts.Clear();
            if (board == null || !board.TryGetBoard(out Board model) || model == null) { IsPlaying = false; return; }
            Vector2 center = board.ContentBounds.center; float far = 0.001f;
            foreach (Arrow arrow in model.Arrows)
                if (board.TryGetArrowView(arrow.Id, out ArrowView view) && view != null)
                { views.Add(view); float d = Vector2.Distance(view.BaseWorldPosition, center); starts.Add(d); far = Mathf.Max(far, d); }
            for (int i = 0; i < starts.Count; i++) starts[i] = Mathf.Max(0, delay) + starts[i] / far * MaxStagger;
            total = Mathf.Max(0, delay) + MaxStagger + ArrowTime; elapsed = 0; IsPlaying = views.Count > 0;
            Apply();
        }

        /// <summary>Lands every arrow immediately.</summary>
        public void Finish()
        {
            for (int i = 0; i < views.Count; i++) if (views[i] != null) { views[i].SetOffset(Vector2.zero); views[i].SetAlpha(1); }
            views.Clear(); starts.Clear(); IsPlaying = false;
        }

        /// <summary>Advances the transition (unscaled time, clamped per frame).</summary>
        public void Tick(float dt)
        {
            if (!IsPlaying || !(dt > 0)) return;
            elapsed += Mathf.Min(dt, 0.05f);
            if (elapsed >= total) { Finish(); return; }
            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < views.Count; i++)
            {
                ArrowView view = views[i]; if (view == null) continue;
                float t = Mathf.Clamp01((elapsed - starts[i]) / ArrowTime);
                // Ease-out-back: a quick drop with a tiny settle.
                float c = 1.4f, u = t - 1, ease = 1 + (c + 1) * u * u * u + c * u * u;
                view.SetOffset(new Vector2(0, Lift * (1 - ease)));
                view.SetAlpha(Mathf.Clamp01(t * 2.2f));
            }
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
    }
}
