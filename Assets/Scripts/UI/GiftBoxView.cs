using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Journey gift: after a Journey level's first clear a gift box drops in and wobbles; OPEN shakes it, the lid
    /// flies off in a burst of light and the reward (hint, undo, coins or an arrow skin) rises out of the box.
    /// The reward is already granted and saved before the animation starts.
    /// </summary>
    public sealed class GiftBoxView : MonoBehaviour
    {
        private enum Phase { Closed, Shaking, Opening, Open }
        private const float DropTime = 0.45f, ShakeTime = 0.7f, OpenTime = 0.7f;
        private RectTransform card, box, lid, rays, reward;
        private UnityEngine.UI.Image raysImage, flash;
        private UnityEngine.UI.RawImage rewardImage;
        private Vector2 lidRest;
        private Phase phase;
        private float time;
        private Action collected;

        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Opens the box.</summary>
        public UnityEngine.UI.Button OpenButton { get; private set; }
        /// <summary>Closes after the reveal.</summary>
        public UnityEngine.UI.Button CollectButton { get; private set; }
        /// <summary>"YOU GOT ..." text.</summary>
        public UnityEngine.UI.Text ResultText { get; private set; }
        /// <summary>The reward shown.</summary>
        public DailyReward Reward { get; private set; }
        /// <summary>True once the reward has been revealed.</summary>
        public bool IsRevealed => phase == Phase.Open;

        /// <summary>Builds the hidden overlay.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("JourneyGift", parent, new Color(0.02f, 0.02f, 0.09f, 0.97f), out var safe);
            card = UiFactory.Column(safe, 900, 1300);
            BrandArt.Text("Title", card, "JOURNEY GIFT", 64, new Vector2(860, 100), new Vector2(0, 540), new Color32(255, 214, 92, 255));
            raysImage = BrandArt.Image("Rays", card, "Gift/gift_rays", new Vector2(980, 980), new Vector2(0, 90)); rays = raysImage.rectTransform;
            NeonKit.Isolate(rays.gameObject);
            box = UiFactory.Rect("Box", card, new Vector2(420, 460), new Vector2(0, 40));
            var hit = box.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = new Color(0, 0, 0, 0);
            BrandArt.Image("Base", box, "Gift/gift_base", new Vector2(360, 300), new Vector2(0, -80));
            var lidImage = BrandArt.Image("Lid", box, "Gift/gift_lid", new Vector2(420, 260), new Vector2(0, 130)); lid = lidImage.rectTransform; lidRest = lid.anchoredPosition;
            OpenButton = box.gameObject.AddComponent<UnityEngine.UI.Button>(); OpenButton.targetGraphic = hit; OpenButton.transition = UnityEngine.UI.Selectable.Transition.None;
            OpenButton.onClick.AddListener(Open);
            rewardImage = UiFactory.Rect("Reward", card, new Vector2(250, 250), new Vector2(0, 120)).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            rewardImage.raycastTarget = false; reward = rewardImage.rectTransform;
            flash = UiFactory.Rect("Flash", Root, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(flash.rectTransform); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            ResultText = BrandArt.Text("Result", card, "TAP THE BOX TO OPEN", 40, new Vector2(860, 120), new Vector2(0, -330), Color.white);
            ResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            CollectButton = UiFactory.Button("COLLECT", card, new Vector2(0, -490), NeonButtonStyle.Primary);
            ((RectTransform)CollectButton.transform).sizeDelta = new Vector2(520, 140);
            CollectButton.onClick.AddListener(Collect);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Shows a closed box holding <paramref name="gift"/>; <paramref name="onCollected"/> runs when it closes.</summary>
        public void Show(DailyReward gift, Action onCollected)
        {
            Reward = gift; collected = onCollected;
            rewardImage.texture = RewardArt.Texture(gift); rewardImage.color = RewardArt.Tint(gift);
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            phase = Phase.Closed; time = 0;
            ResultText.text = "TAP THE BOX TO OPEN"; CollectButton.gameObject.SetActive(false);
            Apply();
        }

        /// <summary>Shakes the box and opens it.</summary>
        public void Open()
        {
            if (!IsOpen || phase != Phase.Closed) return;
            phase = Phase.Shaking; time = 0;
            if (!Application.isPlaying) Finish();
        }

        /// <summary>Jumps to the revealed state.</summary>
        public void Finish() { phase = Phase.Open; time = OpenTime; Reveal(); Apply(); }

        private void Reveal()
        {
            ResultText.text = "YOU GOT " + (Reward != null ? Reward.Label.ToUpperInvariant() : "A GIFT") + "!";
            string kind = RewardArt.KindLabel(Reward); if (kind.Length > 0) ResultText.text += "\n" + kind;
            CollectButton.gameObject.SetActive(true);
        }

        private void Collect() { Hide(); var done = collected; collected = null; done?.Invoke(); }

        /// <summary>Closes the overlay.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }

        /// <summary>Explicit animation driver.</summary>
        public void Tick(float dt)
        {
            if (!IsOpen || !(dt >= 0) || float.IsInfinity(dt)) return;
            time += dt;
            if (phase == Phase.Shaking && time >= ShakeTime) { phase = Phase.Opening; time = 0; }
            else if (phase == Phase.Opening && time >= OpenTime) { phase = Phase.Open; Reveal(); }
            Apply();
        }

        private void Apply()
        {
            float t = time;
            switch (phase)
            {
                case Phase.Closed:
                    float drop = Mathf.Clamp01(t / DropTime);
                    box.localScale = Vector3.one * Motion.BackOut(0.2f, 1f, drop);
                    box.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3.2f) * 4f * drop);
                    lid.anchoredPosition = lidRest + new Vector2(0, Mathf.Abs(Mathf.Sin(t * 3.2f)) * 8f * drop);
                    lid.localRotation = Quaternion.identity; SetAlpha(lid, 1);
                    rays.localScale = Vector3.one * 0.6f; raysImage.color = new Color(1, 0.86f, 0.5f, 0.25f * drop); rays.localRotation = Quaternion.Euler(0, 0, -t * 12f);
                    reward.localScale = Vector3.zero; flash.color = new Color(1, 1, 1, 0);
                    break;
                case Phase.Shaking:
                    float k = t / ShakeTime;
                    box.localScale = Vector3.one * (1f + 0.06f * k);
                    box.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 48f) * (4f + 10f * k));
                    lid.anchoredPosition = lidRest + new Vector2(0, Mathf.Abs(Mathf.Sin(t * 30f)) * 14f * k);
                    raysImage.color = new Color(1, 0.86f, 0.5f, 0.25f + 0.35f * k);
                    break;
                case Phase.Opening:
                case Phase.Open:
                    float o = Mathf.Clamp01(t / OpenTime), e = Motion.Out(o);
                    box.localRotation = Quaternion.identity; box.localScale = Vector3.one * Mathf.Lerp(1.06f, 1f, e);
                    lid.anchoredPosition = lidRest + new Vector2(140f * e, 520f * e - 260f * e * e);
                    lid.localRotation = Quaternion.Euler(0, 0, -50f * e); SetAlpha(lid, 1f - o);
                    rays.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, e); rays.localRotation = Quaternion.Euler(0, 0, -(phase == Phase.Open ? Time.unscaledTime : t) * 18f);
                    raysImage.color = new Color(1, 0.9f, 0.6f, Mathf.Lerp(0.6f, 0.85f, e));
                    reward.anchoredPosition = new Vector2(0, Mathf.Lerp(40f, 300f, e));
                    reward.localScale = Vector3.one * Motion.BackOut(0f, 1f, Mathf.Clamp01(o * 1.4f));
                    flash.color = new Color(1, 1, 1, 0.75f * Mathf.Clamp01(1f - o * 3f));
                    break;
            }
        }

        private static void SetAlpha(RectTransform rect, float alpha)
        {
            var image = rect.GetComponent<UnityEngine.UI.Image>(); var c = image.color; c.a = alpha; image.color = c;
        }

        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f)); }
    }
}
