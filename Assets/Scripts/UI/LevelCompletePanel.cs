using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Final attempt summary and replay/next actions, above the HUD.</summary>
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        private RectTransform overlay, card;
        private float fitScale = 1, popTime = 1;
        /// <summary>Full-screen party-popper confetti shown with the victory card.</summary>
        public ConfettiView Confetti { get; private set; }
        /// <summary>Pops the card in and fires the confetti (victory moment).</summary>
        public void Celebrate()
        {
            if (!IsVisible) return;
            Confetti.transform.SetAsLastSibling(); Confetti.Burst();
            popTime = Application.isPlaying ? 0 : 1; ApplyPop();
        }
        private void ApplyPop()
        {
            float t = Motion.Out(popTime);
            card.localScale = Vector3.one * fitScale * Motion.BackOut(0.25f, 1f, popTime);
            card.localRotation = Quaternion.Euler(0, 0, (1 - t) * -8f);
        }
        private RectTransform safeArea;
        private Vector2 lastSize;
        private CompletionResult presentedRating;
        private CompletionResult coinResult;
        private RectTransform coinLine, trophyLine;
        private int trophyAmount; private bool promoted;
        private string[] trophyFrames;
        private float presentationElapsed;
        private bool coinEnabled,coinEmitted;
        private string[] coinFrames;
        private int coinFrame=-1;
        /// <summary>Accepted reward shown after the stars.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>Trophies won, shown beside the coins.</summary>
        public UnityEngine.UI.Text TrophiesText { get; private set; }
        /// <summary>League promotion banner, shown only when the clear crossed a league threshold.</summary>
        public UnityEngine.UI.Text PromotionText { get; private set; }
        /// <summary>Raised once when reward presentation starts.</summary>
        public event System.Action CoinPresented;
        /// <summary>Supplies the already saved reward; sandbox never shows it.</summary>
        public void ShowCoins(int amount,bool sandbox)=>ShowRewards(amount,0,false,League.Bronze,sandbox);
        /// <summary>Supplies the saved coin and trophy rewards and an optional league promotion.</summary>
        public void ShowRewards(int amount,int trophies,bool promotedNow,League league,bool sandbox)
        {
            if(sandbox||Rating==null){coinEnabled=false;coinLine.gameObject.SetActive(false);trophyLine.gameObject.SetActive(false);PromotionText.gameObject.SetActive(false);return;}
            coinEnabled=true;
            if(ReferenceEquals(coinResult,Rating))return;
            coinResult=Rating;presentationElapsed=0;coinEmitted=false;coinFrame=-1;coinFrames=new string[31];trophyFrames=new string[31];
            trophyAmount=Mathf.Max(0,trophies);promoted=promotedNow&&trophyAmount>0;
            for(int i=0;i<=30;i++){coinFrames[i]="+"+Mathf.RoundToInt(Mathf.Lerp(0,amount,i/30f));trophyFrames[i]="+"+Mathf.RoundToInt(Mathf.Lerp(0,trophyAmount,i/30f));}
            CoinsText.text=coinFrames[0];TrophiesText.text=trophyFrames[0];coinLine.gameObject.SetActive(false);trophyLine.gameObject.SetActive(false);PromotionText.gameObject.SetActive(false);
            // Coins alone stay centred; with trophies the two rewards sit side by side.
            coinLine.anchoredPosition=new Vector2(trophyAmount>0?-130:0,-210);
            if(promoted){PromotionText.text="PROMOTED TO "+LeagueArt.Name(league)+" LEAGUE!";PromotionText.color=LeagueArt.Tint(league);}
        }

        /// <summary>Earned-star presentation.</summary>
        public StarDisplay Stars { get; private set; }
        /// <summary>Attempt taps and fixed par.</summary>
        public UnityEngine.UI.Text MovesText { get; private set; }
        /// <summary>Summary currently presented, used to avoid duplicate animations on event refresh.</summary>
        public CompletionResult Rating { get; private set; }
        /// <summary>Pause flag for presentation and feedback.</summary>
        public bool IsPaused { get; set; }
        /// <summary>Raised at each earned star pop.</summary>
        public event System.Action<int> StarPresented;
        /// <summary>Advances only an active, unpaused presentation.</summary>
        public void Tick(float dt)
        {
            if(!IsVisible||IsPaused||Stars==null||!(dt>0)||float.IsInfinity(dt))return;
            Stars.Tick(dt);
            if(!coinEnabled||Rating==null)return;
            presentationElapsed+=dt;float elapsed=presentationElapsed-Rating.Stars*.25f;
            if(elapsed<0)return;
            if(!coinEmitted){coinEmitted=true;coinLine.gameObject.SetActive(true);trophyLine.gameObject.SetActive(trophyAmount>0);CoinPresented?.Invoke();}
            int current=Mathf.Min(30,Mathf.FloorToInt(elapsed/.6f*30));if(current!=coinFrame){coinFrame=current;CoinsText.text=coinFrames[current];if(trophyFrames!=null)TrophiesText.text=trophyFrames[current];}
            if(promoted&&current==30&&!PromotionText.gameObject.activeSelf){PromotionText.gameObject.SetActive(true);AllCompleteText.gameObject.SetActive(false);}
        }
        private void Update()
        {
            if (!Application.isPlaying) return;
            Tick(Time.deltaTime);
            if (IsVisible && popTime < 1) { popTime = Mathf.Min(1, popTime + Time.unscaledDeltaTime / 0.5f); ApplyPop(); }
        }
        /// <summary>Adds star results to the unchanged completion actions and paired-best labels.</summary>
        public void ShowRating(CompletionResult result, bool sandbox)
        {
            if (result == null || result.Par < 1) return;
            Rating = result;
            Stars.gameObject.SetActive(true);
            if (!ReferenceEquals(presentedRating, result)) { presentedRating = result; Stars.Pop(result.Stars); }
            MovesText.text = $"{result.Moves} / {result.Par} moves";
            MovesText.gameObject.SetActive(true);
            BestNoticeText.text = sandbox ? "Practice - not saved" : result.IsFirstClear ? "First clear!" : result.IsNewBestStars ? "New best!" : "";
            BestNoticeText.gameObject.SetActive(sandbox || result.IsFirstClear || result.IsNewBestStars);
        }

        /// <summary>Whether the full-screen modal overlay is visible.</summary>
        public bool IsVisible => overlay != null && overlay.gameObject.activeSelf;
        /// <summary>Mistake summary.</summary>
        public UnityEngine.UI.Text MistakesText { get; private set; }
        /// <summary>Undo summary.</summary>
        public UnityEngine.UI.Text UndosText { get; private set; }
        /// <summary>Perfect-attempt label, visible only with zero mistakes and undos.</summary>
        public UnityEngine.UI.Text FlawlessText { get; private set; }
        /// <summary>The paired best attempt.</summary>
        public UnityEngine.UI.Text BestText { get; private set; }
        /// <summary>First-clear or strict-improvement label.</summary>
        public UnityEngine.UI.Text BestNoticeText { get; private set; }
        /// <summary>Final-list completion message.</summary>
        public UnityEngine.UI.Text AllCompleteText { get; private set; }
        /// <summary>Replay button.</summary>
        public UnityEngine.UI.Button ReplayButton { get; private set; }
        /// <summary>Next level button.</summary>
        public UnityEngine.UI.Button NextButton { get; private set; }

        /// <summary>Level selection action.</summary>
        public UnityEngine.UI.Button LevelsButton { get; private set; }
        /// <summary>Raised when level selection is requested from a visible completion.</summary>
        public event System.Action LevelsRequested;
        /// <summary>Back to the home page (Step 14M).</summary>
        public UnityEngine.UI.Button HomeButton { get; private set; }
        /// <summary>Raised by <see cref="HomeButton"/> while the card is visible.</summary>
        public event System.Action HomeRequested;
        /// <summary>Builds the initially hidden overlay once.</summary>
        public void Build(RectTransform canvas, RectTransform safe)
        {
            if (overlay != null) return;
            safeArea = safe;
            overlay = UiFactory.Panel("LevelCompleteOverlay", canvas, ViewTheme.PanelDim, true);
            card = UiFactory.Panel("Card", overlay, ViewTheme.PanelCard);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(900, 1160);
            card.anchoredPosition = Vector2.zero;
            Confetti = UiFactory.Rect("Confetti", overlay, Vector2.zero, Vector2.zero).gameObject.AddComponent<ConfettiView>();
            UiFactory.Stretch(Confetti.rectTransform); Confetti.raycastTarget = false; NeonKit.Isolate(Confetti.gameObject);
            BrandArt.Text("Title", card, "LEVEL COMPLETE", 58, new Vector2(850, 90), new Vector2(0, 490), new Color32(255, 214, 92, 255));
            var starRoot = UiFactory.Rect("EarnedStars", card, new Vector2(450,120), new Vector2(0,380));
            Stars = starRoot.gameObject.AddComponent<StarDisplay>(); Stars.Build(card, 110, 150);
            starRoot.anchoredPosition = new Vector2(0,380);
            Stars.Presented += OnStarPresented;
            MovesText = UiFactory.Text("StarMoves", card, "", 40, new Vector2(850,60), new Vector2(0,290));
            MistakesText = UiFactory.Text("Mistakes", card, "", 44, new Vector2(850, 65), new Vector2(0, 210));
            UndosText = UiFactory.Text("Undos", card, "", 44, new Vector2(850, 65), new Vector2(0, 145));
            FlawlessText = UiFactory.Text("Flawless", card, "Flawless!", 44, new Vector2(850, 65), new Vector2(0, 75));
            BestText = UiFactory.Text("Best", card, "", 38, new Vector2(850, 65), new Vector2(0, 10));
            BestNoticeText = UiFactory.Text("BestNotice", card, "", 42, new Vector2(850, 65), new Vector2(0, -65));
            AllCompleteText = UiFactory.Text("AllComplete", card, "All levels complete!", 42, new Vector2(850, 65), new Vector2(0, -145));
            coinLine=UiFactory.Rect("CoinReward",card,new Vector2(220,48),new Vector2(0,-210));
            UiStyle.Icon("Coin",coinLine,UiSpriteShape.Coin,new Vector2(-62,0),42,UiStyle.Gold);
            CoinsText=UiFactory.Text("Coins",coinLine,"",40,new Vector2(150,48),new Vector2(40,0));CoinsText.color=UiStyle.Ink;CoinsText.verticalOverflow=VerticalWrapMode.Overflow;CoinsText.horizontalOverflow=HorizontalWrapMode.Overflow;CoinsText.alignment=TextAnchor.MiddleLeft;
            coinLine.gameObject.SetActive(false);
            trophyLine=UiFactory.Rect("TrophyReward",card,new Vector2(220,48),new Vector2(130,-210));
            NeonKit.Icon("Trophy",trophyLine,"icon_trophy",50,new Vector2(-62,2));
            TrophiesText=UiFactory.Text("Trophies",trophyLine,"",40,new Vector2(150,48),new Vector2(40,0));TrophiesText.color=new Color32(255,214,92,255);TrophiesText.verticalOverflow=VerticalWrapMode.Overflow;TrophiesText.horizontalOverflow=HorizontalWrapMode.Overflow;TrophiesText.alignment=TextAnchor.MiddleLeft;
            trophyLine.gameObject.SetActive(false);
            PromotionText=BrandArt.Text("Promotion",card,"",34,new Vector2(850,60),new Vector2(0,-145),Color.white);
            PromotionText.gameObject.SetActive(false);
            ReplayButton = UiFactory.Button("Replay", card, new Vector2(-275, -330));
            NextButton = UiFactory.Button("Next Level", card, new Vector2(0, -330), NeonButtonStyle.Primary);
            LevelsButton = UiFactory.Button("Levels", card, new Vector2(275, -330));
            foreach (var button in new[] { ReplayButton, NextButton, LevelsButton })
                ((RectTransform)button.transform).sizeDelta = new Vector2(260, 130);
            NeonKit.WithIcon(ReplayButton, "icon_restart", 50); NeonKit.WithIcon(NextButton, "icon_next", 50); NeonKit.WithIcon(LevelsButton, "icon_levels", 54);
            FlawlessText.color = new Color32(125, 255, 176, 255); BestNoticeText.color = new Color32(255, 214, 92, 255);
            LevelsButton.onClick.AddListener(() => { if (IsVisible) LevelsRequested?.Invoke(); });
            HomeButton = UiFactory.Button("Home", card, new Vector2(0, -475));
            ((RectTransform)HomeButton.transform).sizeDelta = new Vector2(360, 110); NeonKit.WithIcon(HomeButton, "icon_home", 52);
            HomeButton.onClick.AddListener(() => { if (IsVisible) HomeRequested?.Invoke(); });
            BestText.gameObject.SetActive(false);
            BestNoticeText.gameObject.SetActive(false);
            AllCompleteText.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>Shows final counters, with flawless only for an unaided perfect attempt.</summary>
        public void Show(int mistakes, int undos)
        {
            Rating = null; coinEnabled=false;coinLine.gameObject.SetActive(false);trophyLine.gameObject.SetActive(false);PromotionText.gameObject.SetActive(false);trophyAmount=0;promoted=false; Stars.gameObject.SetActive(false); MovesText.gameObject.SetActive(false);
            BestText.gameObject.SetActive(false);
            BestNoticeText.gameObject.SetActive(false);
            AllCompleteText.gameObject.SetActive(false);
            NextButton.gameObject.SetActive(true);
            NextButton.interactable = true;
            MistakesText.text = $"Mistakes {mistakes}";
            UndosText.text = $"Undos {undos}";
            FlawlessText.gameObject.SetActive(mistakes == 0 && undos == 0);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            UpdateLayout();
        }
        /// <summary>Shows persisted best and flow state without changing this attempt's flawless rules.</summary>
        public void Show(int mistakes, int undos, CompletionResult result, bool isLastLevel, bool allCompleted, bool canGoNext)
        {
            Show(mistakes, undos);
            if (result != null)
            {
                BestText.text = $"Best: {result.BestMistakes} mistakes" + (result.BestUndos > 0 ? $", {result.BestUndos} undos" : "");
                BestText.gameObject.SetActive(true);
                BestNoticeText.text = result.IsFirstClear ? "First clear!" : result.IsNewBest ? "New best!" : "";
                BestNoticeText.gameObject.SetActive(result.IsFirstClear || result.IsNewBest);
            }
            NextButton.gameObject.SetActive(!isLastLevel);
            NextButton.interactable = canGoNext;
            AllCompleteText.gameObject.SetActive(isLastLevel && allCompleted);
        }
        /// <summary>Hides the completion overlay.</summary>
        public void Hide() { if (overlay != null) overlay.gameObject.SetActive(false); }
        private void OnStarPresented(int index) => StarPresented?.Invoke(index);
        private void LateUpdate() { if (IsVisible) UpdateLayout(); }
        private void UpdateLayout()
        {
            Vector2 size = safeArea.rect.size;
            if (lastSize == size) return;
            lastSize = size;
            fitScale = Mathf.Min(1, size.x * 0.9f / 900, size.y * 0.9f / 1160); ApplyPop();
            card.position = safeArea.TransformPoint(safeArea.rect.center);
        }
    }
}
