using System;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Three reusable procedural star slots with allocation-free pop and loss animations.</summary>
    public sealed class StarDisplay : MonoBehaviour
    {
        private readonly UnityEngine.UI.Image[] images = new UnityEngine.UI.Image[3];
        private readonly Vector2[] positions = new Vector2[3];
        private readonly float[] shakes = new float[3];
        private float elapsed;
        private int emitted;
        private bool popping;
        /// <summary>Number of earned or potential stars.</summary>
        public int Count { get; private set; }
        /// <summary>Raised when each earned star starts its pop, once per presentation.</summary>
        public event Action<int> Presented;
        /// <summary>Read-only access to a slot for visual verification.</summary>
        public UnityEngine.UI.Image Slot(int index) => images[index];
        /// <summary>Builds slots once at fixed local positions.</summary>
        public void Build(Transform parent, float size, float spacing)
        {
            if (images[0] != null) return;
            transform.SetParent(parent, false);
            for (int i = 0; i < 3; i++)
            {
                positions[i] = new Vector2((i - 1) * spacing, 0);
                images[i] = UiFactory.Rect("Star" + (i + 1), transform, new Vector2(size,size), positions[i]).gameObject.AddComponent<UnityEngine.UI.Image>();
                images[i].raycastTarget = false;
            }
            SetCount(3);
        }
        /// <summary>Changes the meter; only newly lost stars shake.</summary>
        public void SetCount(int count, bool shake = false)
        {
            popping = false;
            int previous = Count; Count = Mathf.Clamp(count, 0, 3);
            for (int i = 0; i < 3; i++)
            {
                images[i].sprite = SpriteFactory.GetStarSprite(i >= Count);
                images[i].color = i < Count ? ViewTheme.StarGold : ViewTheme.StarGrey;
                images[i].rectTransform.localScale = Vector3.one;
                images[i].rectTransform.anchoredPosition = positions[i];
                shakes[i] = shake && i >= Count && i < previous ? .3f : 0;
            }
        }
        /// <summary>Starts a new sequential presentation; missing stars remain outlined.</summary>
        public void Pop(int count)
        {
            SetCount(count); elapsed = 0; emitted = 0; popping = true;
            for (int i = 0; i < Count; i++) images[i].rectTransform.localScale = Vector3.zero;
        }
        /// <summary>Advances animations without temporary buffers, strings or delegates.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) return;
            if (popping)
            {
                elapsed += dt;
                while (emitted < Count && elapsed >= emitted * .25f) { int index = emitted++; Presented?.Invoke(index); }
                for (int i = 0; i < Count; i++)
                {
                    float t = Mathf.Clamp01((elapsed - i * .25f) / .25f);
                    float scale = t < .7f ? 1.15f * (1 - Mathf.Pow(1 - t / .7f, 3)) : Mathf.Lerp(1.15f, 1, (t - .7f) / .3f);
                    images[i].rectTransform.localScale = Vector3.one * scale;
                }
                if (elapsed >= Count * .25f) popping = false;
            }
            for (int i = 0; i < 3; i++) if (shakes[i] > 0)
            {
                shakes[i] = Mathf.Max(0, shakes[i] - dt);
                float offset = shakes[i] == 0 ? 0 : Mathf.Sin(shakes[i] * 65) * 4 * (shakes[i] / .3f);
                images[i].rectTransform.anchoredPosition = positions[i] + new Vector2(offset, 0);
            }
        }
    }
}
