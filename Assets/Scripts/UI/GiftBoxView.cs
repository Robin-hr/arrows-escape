using System;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Journey gift (Step 14L): a real 3D gift box (pre-rendered rotation and opening frames, Brand/Gift/gift3d_atlas)
    /// drops in with a bounce and turns slowly while it floats. Tapping it makes it spin faster and shake, then the lid
    /// bursts off: a white flash, confetti, golden light rays and sparkles, and the reward (hint, undo, coins or an
    /// arrow skin) rises out of the glowing box. The reward is already granted and saved before the animation starts.
    /// </summary>
    public sealed class GiftBoxView : MonoBehaviour
    {
        private enum Phase { Closed, Shaking, Opening, Open }
        /// <summary>Frames in the closed turntable loop.</summary>
        public const int SpinFrames = 32;
        /// <summary>Frames in the opening sequence.</summary>
        public const int OpenFrames = 24;
        private const int AtlasColumns = 8, AtlasRows = 7, FramePixels = 256, Sparkles = 10;
        private const float DropTime = 0.6f, ShakeTime = 0.75f, OpenTime = 0.9f, LidPop = 0.18f;
        private static Sprite[] frames;
        private RectTransform card, box, rays, reward, shadow;
        private UnityEngine.UI.Image boxImage, raysImage, flash, glow;
        private UnityEngine.UI.RawImage rewardImage;
        private readonly RectTransform[] sparkles = new RectTransform[Sparkles];
        private readonly UnityEngine.UI.Image[] sparkleImages = new UnityEngine.UI.Image[Sparkles];
        private ConfettiView confetti;
        private Phase phase;
        private float time, spin;
        private bool burst;
        private Action collected;

        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Opens the box (tap target over the 3D box).</summary>
        public UnityEngine.UI.Button OpenButton { get; private set; }
        /// <summary>Closes after the reveal.</summary>
        public UnityEngine.UI.Button CollectButton { get; private set; }
        /// <summary>"YOU GOT ..." text.</summary>
        public UnityEngine.UI.Text ResultText { get; private set; }
        /// <summary>The reward shown.</summary>
        public DailyReward Reward { get; private set; }
        /// <summary>True once the reward has been revealed.</summary>
        public bool IsRevealed => phase == Phase.Open;
        /// <summary>Atlas frame currently shown (0..31 spin, 32..55 opening).</summary>
        public int Frame { get; private set; }
        /// <summary>Confetti burst when the lid pops.</summary>
        public ConfettiView Confetti => confetti;

        private static void LoadFrames()
        {
            if (frames != null && frames[0] != null) return;
            frames = new Sprite[SpinFrames + OpenFrames];
            var texture = BrandArt.Texture("Gift/gift3d_atlas"); if (texture == null) return;
            float scale = texture.width / (float)(AtlasColumns * FramePixels);
            for (int i = 0; i < frames.Length; i++)
            {
                int x = i % AtlasColumns, y = AtlasRows - 1 - i / AtlasColumns;
                frames[i] = Sprite.Create(texture, new Rect(x * FramePixels * scale, y * FramePixels * scale, FramePixels * scale, FramePixels * scale), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                frames[i].hideFlags = HideFlags.HideAndDontSave;
            }
        }

        /// <summary>Builds the hidden overlay.</summary>
        public void Build(Transform parent)
        {
            LoadFrames();
            Root = UiFactory.ScreenRoot("JourneyGift", parent, new Color(0.02f, 0.02f, 0.09f, 0.97f), out var safe);
            // A fully opaque backdrop so the level-complete card underneath never shows through the gift scene.
            var cover = UiFactory.Rect("Backdrop", Root, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(cover.rectTransform); cover.color = new Color(0.03f, 0.02f, 0.1f, 1f); cover.raycastTarget = false; cover.rectTransform.SetAsFirstSibling();
            card = UiFactory.Column(safe, 900, 1300);
            BrandArt.Text("Title", card, "JOURNEY GIFT", 64, new Vector2(860, 100), new Vector2(0, 540), new Color32(255, 214, 92, 255));
            raysImage = BrandArt.Image("Rays", card, "Gift/gift_rays", new Vector2(1000, 1000), new Vector2(0, 80)); rays = raysImage.rectTransform;
            NeonKit.Isolate(rays.gameObject);
            glow = BrandArt.Image("Glow", card, "Glass/avatar_glow", new Vector2(620, 620), new Vector2(0, 40)); glow.color = new Color(1f, 0.75f, 0.35f, 0.25f);
            shadow = BrandArt.Image("Shadow", card, "Journey/jn_glow", new Vector2(360, 90), new Vector2(0, -190)).rectTransform;
            shadow.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);
            box = UiFactory.Rect("Box", card, new Vector2(560, 560), new Vector2(0, 60));
            boxImage = box.gameObject.AddComponent<UnityEngine.UI.Image>(); boxImage.preserveAspect = true;
            if (frames != null && frames[0] != null) boxImage.sprite = frames[0]; else boxImage.color = new Color(0.86f, 0.12f, 0.55f, 1);
            NeonKit.Isolate(box.gameObject); box.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            OpenButton = box.gameObject.AddComponent<UnityEngine.UI.Button>(); OpenButton.targetGraphic = boxImage; OpenButton.transition = UnityEngine.UI.Selectable.Transition.None;
            OpenButton.onClick.AddListener(Open);
            rewardImage = UiFactory.Rect("Reward", card, new Vector2(260, 260), new Vector2(0, 120)).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            rewardImage.raycastTarget = false; reward = rewardImage.rectTransform;
            for (int i = 0; i < Sparkles; i++)
            {
                sparkleImages[i] = BrandArt.Image("Sparkle" + i, card, "Glass/avatar_glow", new Vector2(44, 44), Vector2.zero);
                sparkles[i] = sparkleImages[i].rectTransform;
            }
            confetti = UiFactory.Rect("Confetti", Root, Vector2.zero, Vector2.zero).gameObject.AddComponent<ConfettiView>();
            UiFactory.Stretch(confetti.rectTransform); confetti.raycastTarget = false; NeonKit.Isolate(confetti.gameObject);
            flash = UiFactory.Rect("Flash", Root, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(flash.rectTransform); flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            ResultText = BrandArt.Text("Result", card, "TAP THE BOX TO OPEN", 40, new Vector2(860, 120), new Vector2(0, -330), Color.white);
            ResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            CollectButton = UiFactory.Button("COLLECT", card, new Vector2(0, -490), NeonButtonStyle.Primary);
            ((RectTransform)CollectButton.transform).sizeDelta = new Vector2(520, 140);
            CollectButton.onClick.AddListener(Collect);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Shows the closed box holding <paramref name="gift"/>; <paramref name="onCollected"/> runs when it closes.</summary>
        public void Show(DailyReward gift, Action onCollected)
        {
            LoadFrames();
            Reward = gift; collected = onCollected;
            rewardImage.texture = RewardArt.Texture(gift); rewardImage.color = RewardArt.Tint(gift);
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            phase = Phase.Closed; time = 0; spin = 0; burst = false;
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
            spin += dt * (phase == Phase.Shaking ? 34f + 40f * Mathf.Clamp01(time / ShakeTime) : 11f);
            if (phase == Phase.Shaking && time >= ShakeTime) { phase = Phase.Opening; time = 0; }
            else if (phase == Phase.Opening && time >= OpenTime) { phase = Phase.Open; Reveal(); }
            if (phase == Phase.Opening && !burst && time >= LidPop) { burst = true; confetti.Burst(); }
            Apply();
        }

        private void ShowFrame(int index)
        {
            Frame = index;
            if (frames == null || index < 0 || index >= frames.Length || frames[index] == null) return;
            if (boxImage.sprite != frames[index]) boxImage.sprite = frames[index];
        }

        private void Apply()
        {
            float t = time;
            float clock = Application.isPlaying ? Time.unscaledTime : t;
            switch (phase)
            {
                case Phase.Closed:
                {
                    float drop = Mathf.Clamp01(t / DropTime), fall = Motion.BackOut(0f, 1f, drop);
                    float bob = Mathf.Sin(t * 2.4f) * 12f * drop;
                    box.anchoredPosition = new Vector2(0, 60 + (1 - fall) * 700f + bob);
                    box.localScale = new Vector3(1 + 0.08f * (1 - drop) * Mathf.Sin(drop * 9f), 1 - 0.08f * (1 - drop) * Mathf.Sin(drop * 9f), 1);
                    box.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.7f) * 3f * drop);
                    ShowFrame((int)spin % SpinFrames);
                    shadow.localScale = new Vector3(1f - bob / 120f, 1f - bob / 120f, 1) * drop;
                    rays.localScale = Vector3.one * 0.55f; raysImage.color = new Color(1, 0.86f, 0.5f, 0.22f * drop); rays.localRotation = Quaternion.Euler(0, 0, -clock * 12f);
                    glow.color = new Color(1f, 0.75f, 0.35f, 0.18f + 0.08f * Mathf.Sin(t * 3f));
                    reward.localScale = Vector3.zero; flash.color = new Color(1, 1, 1, 0);
                    SetSparkles(0, t, 0);
                    break;
                }
                case Phase.Shaking:
                {
                    float k = Mathf.Clamp01(t / ShakeTime);
                    box.anchoredPosition = new Vector2(Mathf.Sin(t * 61f) * 10f * k, 60 + Mathf.Abs(Mathf.Sin(t * 23f)) * 22f * k);
                    box.localScale = Vector3.one * (1f + 0.1f * k + 0.04f * Mathf.Sin(t * 40f));
                    box.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 47f) * (3f + 9f * k));
                    ShowFrame((int)spin % SpinFrames);
                    raysImage.color = new Color(1, 0.86f, 0.5f, 0.22f + 0.4f * k); rays.localScale = Vector3.one * (0.55f + 0.2f * k);
                    rays.localRotation = Quaternion.Euler(0, 0, -clock * (12f + 60f * k));
                    glow.color = new Color(1f, 0.8f, 0.4f, 0.2f + 0.4f * k);
                    SetSparkles(k, t, 0.6f * k);
                    break;
                }
                case Phase.Opening:
                case Phase.Open:
                {
                    float o = Mathf.Clamp01(t / OpenTime), e = Motion.Out(o);
                    ShowFrame(SpinFrames + Mathf.Min(OpenFrames - 1, (int)(o * OpenFrames)));
                    box.anchoredPosition = new Vector2(0, 60); box.localRotation = Quaternion.identity;
                    float punch = t < LidPop ? 1f - 0.12f * (t / LidPop) : Motion.BackOut(0.88f, 1f, Mathf.Clamp01((t - LidPop) / 0.35f));
                    box.localScale = Vector3.one * punch;
                    rays.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.2f, e); rays.localRotation = Quaternion.Euler(0, 0, -clock * 22f);
                    raysImage.color = new Color(1, 0.9f, 0.6f, Mathf.Lerp(0.65f, 0.9f, e));
                    glow.color = new Color(1f, 0.85f, 0.45f, 0.65f - 0.25f * e);
                    float rise = Mathf.Clamp01((t - LidPop) / 0.6f);
                    float floatY = phase == Phase.Open ? Mathf.Sin(clock * 2.2f) * 10f : 0f;
                    reward.anchoredPosition = new Vector2(0, Mathf.Lerp(60f, 330f, Motion.Out(rise)) + floatY);
                    reward.localScale = Vector3.one * Motion.BackOut(0f, 1f, rise);
                    reward.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 1.6f) * 5f);
                    float f = t - LidPop; flash.color = new Color(1, 1, 1, f < 0 ? 0 : 0.85f * Mathf.Clamp01(1f - f * 3.5f));
                    SetSparkles(1f, clock, 1f);
                    break;
                }
            }
        }

        // Golden sparkles orbiting the box (and the reward once it is out).
        private void SetSparkles(float amount, float clock, float burstOut)
        {
            for (int i = 0; i < Sparkles; i++)
            {
                bool on = amount > 0.01f;
                if (sparkles[i].gameObject.activeSelf != on) sparkles[i].gameObject.SetActive(on);
                if (!on) continue;
                float a = clock * (1.2f + 0.15f * (i % 3)) + i * Mathf.PI * 2f / Sparkles;
                float radius = 230f + 90f * burstOut + 25f * Mathf.Sin(clock * 2f + i);
                sparkles[i].anchoredPosition = new Vector2(Mathf.Cos(a) * radius, 120f + Mathf.Sin(a) * radius * 0.55f);
                float tw = 0.5f + 0.5f * Mathf.Sin(clock * 6f + i * 1.7f);
                sparkles[i].localScale = Vector3.one * (0.5f + 0.8f * tw) * amount;
                sparkleImages[i].color = new Color(1f, 0.9f - 0.2f * (i % 2), 0.55f, (0.4f + 0.6f * tw) * amount);
            }
        }

        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f)); }
    }
}
