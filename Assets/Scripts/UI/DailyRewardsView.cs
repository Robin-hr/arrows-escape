using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Seven-day reward calendar. Days 1-6 are fixed; day 7 is a mystery gift revealed by a spinning reel that
    /// slows down and lands on the reward. Claimed rewards are applied and saved immediately.
    /// </summary>
    public sealed class DailyRewardsView : MonoBehaviour
    {
        /// <summary>Length of the day-7 reel animation in seconds.</summary>
        public const float ReelSeconds = 2.6f;
        private readonly UnityEngine.UI.Image[] cards = new UnityEngine.UI.Image[7];
        private readonly UnityEngine.UI.Image[] checks = new UnityEngine.UI.Image[7];
        private readonly UnityEngine.UI.Text[] labels = new UnityEngine.UI.Text[7];
        private Func<bool> claim;
        private UnityEngine.UI.RawImage reelIcon;
        private UnityEngine.UI.Text reelLabel, resultText;
        private RectTransform reelRoot;
        private List<DailyReward> reelPool;
        private DailyReward reelResult;
        private float reelTime, stepTimer;
        private int reelIndex;
        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;
        /// <summary>Back to home.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Claims today's reward.</summary>
        public UnityEngine.UI.Button ClaimButton { get; private set; }
        /// <summary>True while the day-7 reel spins.</summary>
        public bool IsSpinning { get; private set; }
        /// <summary>The reward revealed by the last claim.</summary>
        public DailyReward LastReward { get; private set; }
        /// <summary>Result line after a claim.</summary>
        public UnityEngine.UI.Text ResultText => resultText;

        /// <summary>Builds the hidden page; <paramref name="onClaim"/> performs the claim and returns true on success.</summary>
        public void Build(Transform parent, Func<bool> onClaim, Action onBack)
        {
            claim = onClaim;
            Root = UiFactory.ScreenRoot("DailyRewards", parent, BrandArt.Space, out var safe);
            NeonKit.Backdrop(Root, 0.55f);
            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            BackButton = NeonKit.IconButton("Back", header, "icon_back", new Vector2(-420, -62), 112, 56);
            BackButton.onClick.AddListener(() => { if (!IsSpinning) onBack?.Invoke(); });
            BrandArt.Text("Title", header, "DAILY REWARDS", 58, new Vector2(700, 100), new Vector2(40, -62), Color.white);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }

            var area = UiFactory.Rect("RewardsContent", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(area); area.offsetMax = new Vector2(0, -180);
            var column = UiFactory.Column(area, 940, 1600);
            BrandArt.Text("Subtitle", column, "Come back every day for a new reward", 30, new Vector2(900, 50), new Vector2(0, 750), BrandArt.Lavender);
            for (int day = 0; day < 6; day++)
            {
                int row = day / 3, col = day % 3; var pos = new Vector2((col - 1) * 300, 520 - row * 370);
                cards[day] = Card(column, day, new Vector2(280, 340), pos);
                var reward = DailyRewardRules.Preview(day);
                var icon = UiFactory.Rect("Icon", cards[day].transform, new Vector2(130, 130), new Vector2(0, 10)).gameObject.AddComponent<UnityEngine.UI.RawImage>();
                icon.texture = RewardArt.Texture(reward); icon.raycastTarget = false;
                labels[day] = BrandArt.Text("Amount", cards[day].transform, reward.Label, 26, new Vector2(260, 50), new Vector2(0, -115), Color.white);
            }
            cards[6] = Card(column, 6, new Vector2(880, 380), new Vector2(0, -280));
            reelRoot = UiFactory.Rect("Reel", cards[6].transform, new Vector2(860, 300), new Vector2(0, -20));
            reelIcon = UiFactory.Rect("Icon", reelRoot, new Vector2(190, 190), new Vector2(-200, 0)).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            reelIcon.texture = BrandArt.Texture("Daily/gift_box"); reelIcon.raycastTarget = false;
            reelLabel = BrandArt.Text("Label", reelRoot, "MYSTERY GIFT", 38, new Vector2(460, 120), new Vector2(150, 0), new Color32(255, 214, 92, 255));
            reelLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            labels[6] = reelLabel;
            ClaimButton = UiFactory.Button("CLAIM", column, new Vector2(0, -600), NeonButtonStyle.Primary);
            ((RectTransform)ClaimButton.transform).sizeDelta = new Vector2(680, 150); NeonKit.WithIcon(ClaimButton, "Daily/icon_gift", 74);
            ClaimButton.onClick.AddListener(OnClaim);
            resultText = BrandArt.Text("Result", column, "", 32, new Vector2(900, 60), new Vector2(0, -730), new Color32(125, 255, 176, 255));
            Root.gameObject.SetActive(false);
        }

        private UnityEngine.UI.Image Card(Transform parent, int day, Vector2 size, Vector2 pos)
        {
            var card = UiFactory.Rect("Day" + (day + 1), parent, size, pos).gameObject.AddComponent<UnityEngine.UI.Image>();
            NeonKit.Panel(card, NeonKit.Cyan); card.raycastTarget = false;
            BrandArt.Text("DayLabel", card.transform, "DAY " + (day + 1), 30, new Vector2(size.x - 20, 50), new Vector2(0, size.y * 0.5f - 42), BrandArt.Cyan);
            checks[day] = NeonKit.Icon("Claimed", card.transform, "icon_check", 90, new Vector2(size.x * 0.5f - 52, size.y * 0.5f - 52));
            return card;
        }

        /// <summary>Fills and opens the page.</summary>
        public void Show(Inventory inventory, int todayKey)
        {
            if (Root == null) return;
            Refresh(inventory, todayKey); resultText.text = "";
            if (!IsSpinning) { reelIcon.texture = BrandArt.Texture("Daily/gift_box"); reelIcon.color = Color.white; reelLabel.text = "MYSTERY GIFT"; }
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        private void Refresh(Inventory inventory, int todayKey)
        {
            bool canClaim = DailyRewardRules.CanClaim(inventory, todayKey);
            int next = DailyRewardRules.NextDay(inventory);
            // Days before "next" in this cycle are done; when today is claimed the one just taken shows as done too.
            for (int day = 0; day < 7; day++)
            {
                bool done = day < next || (!canClaim && next == 0 && inventory.DailyRewardLastKey != 0);
                bool today = canClaim && day == next;
                NeonKit.SetRim(cards[day].transform, "neon_panel_rim", NeonKit.PanelBorder, today ? NeonKit.Gold : done ? NeonKit.Dim : NeonKit.Cyan);
                checks[day].gameObject.SetActive(done);
                cards[day].color = done ? new Color(1, 1, 1, 0.6f) : Color.white;
            }
            ClaimButton.interactable = canClaim && !IsSpinning;
            ClaimButton.GetComponentInChildren<UnityEngine.UI.Text>().text = canClaim ? "CLAIM DAY " + (next + 1) : "COME BACK TOMORROW";
        }

        private void OnClaim()
        {
            if (IsSpinning || claim == null || !claim()) return;
        }

        /// <summary>Called by the flow after a successful claim: shows the result, spinning the reel for day 7.</summary>
        public void Present(DailyReward reward, int day, Inventory inventory, int todayKey)
        {
            LastReward = reward;
            Refresh(inventory, todayKey);
            if (day == 6)
            {
                reelPool = DailyRewardRules.MysteryPool(inventory); if (reelPool.Count == 0) reelPool.Add(reward);
                reelResult = reward; reelTime = 0; stepTimer = 0; reelIndex = 0; IsSpinning = true; ClaimButton.interactable = false; resultText.text = "";
                ShowReel(reelPool[0]);
                if (!Application.isPlaying) FinishReel();
                return;
            }
            resultText.text = "YOU GOT " + reward.Label + "!";
        }

        private void ShowReel(DailyReward reward)
        {
            reelIcon.texture = RewardArt.Texture(reward); reelIcon.color = RewardArt.Tint(reward);
            string kind = RewardArt.KindLabel(reward); reelLabel.text = reward.Label + (kind.Length > 0 ? "\n<size=24>" + kind + "</size>" : "");
        }

        /// <summary>Stops the reel on the reward.</summary>
        public void FinishReel()
        {
            if (!IsSpinning) return;
            IsSpinning = false; ShowReel(reelResult); reelRoot.localScale = Vector3.one;
            resultText.text = "YOU GOT " + reelResult.Label + "!";
        }

        /// <summary>Advances the reel: fast at first, slowing down, then a small pop on the final reward.</summary>
        public void Tick(float dt)
        {
            if (!IsSpinning || !(dt > 0)) return;
            reelTime += dt; stepTimer += dt;
            float t = Mathf.Clamp01(reelTime / ReelSeconds), interval = Mathf.Lerp(0.05f, 0.4f, t * t);
            if (stepTimer >= interval) { stepTimer = 0; reelIndex = (reelIndex + 1) % reelPool.Count; ShowReel(reelPool[reelIndex]); }
            reelRoot.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(reelTime * 30f) * (1 - t));
            if (reelTime >= ReelSeconds) FinishReel();
        }

        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }

        /// <summary>Hides the page (finishing any reel).</summary>
        public void Hide() { FinishReel(); if (Root != null) Root.gameObject.SetActive(false); }
    }
}
