using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Final attempt summary and replay/next actions, above the HUD.</summary>
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        private RectTransform overlay, card;
        private RectTransform safeArea;
        private Vector2 lastSize;
        private CompletionResult presentedRating;
        private CompletionResult coinResult;
        private RectTransform coinLine;
        private float presentationElapsed;
        private bool coinEnabled,coinEmitted;
        private string[] coinFrames;
        private int coinFrame=-1;
        /// <summary>Accepted reward shown after the stars.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>Raised once when reward presentation starts.</summary>
        public event System.Action CoinPresented;
        /// <summary>Supplies the already saved reward; sandbox never shows it.</summary>
        public void ShowCoins(int amount,bool sandbox)
        {
            if(sandbox||Rating==null){coinEnabled=false;coinLine.gameObject.SetActive(false);return;}
            coinEnabled=true;
            if(ReferenceEquals(coinResult,Rating))return;
            coinResult=Rating;presentationElapsed=0;coinEmitted=false;coinFrame=-1;coinFrames=new string[31];
            for(int i=0;i<=30;i++)coinFrames[i]="+"+Mathf.RoundToInt(Mathf.Lerp(0,amount,i/30f));
            CoinsText.text=coinFrames[0];coinLine.gameObject.SetActive(false);
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
            if(!coinEmitted){coinEmitted=true;coinLine.gameObject.SetActive(true);CoinPresented?.Invoke();}
            int current=Mathf.Min(30,Mathf.FloorToInt(elapsed/.6f*30));if(current!=coinFrame){coinFrame=current;CoinsText.text=coinFrames[current];}
        }
        private void Update() { if (Application.isPlaying) Tick(Time.deltaTime); }
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
            UiFactory.Text("Title", card, "Level Complete", 60, new Vector2(850, 90), new Vector2(0, 490)).color = ViewTheme.UiAccent;
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
            CoinsText=UiFactory.Text("Coins",coinLine,"",40,new Vector2(150,48),new Vector2(28,0));CoinsText.color=UiStyle.Ink;
            coinLine.gameObject.SetActive(false);
            ReplayButton = UiFactory.Button("Replay", card, new Vector2(-270, -300));
            NextButton = UiFactory.Button("Next Level", card, new Vector2(0, -300));
            LevelsButton = UiFactory.Button("Levels", card, new Vector2(270, -300));
            foreach (var button in new[] { ReplayButton, NextButton, LevelsButton })
                ((RectTransform)button.transform).sizeDelta = new Vector2(240, 120);
            LevelsButton.onClick.AddListener(() => { if (IsVisible) LevelsRequested?.Invoke(); });
            BestText.gameObject.SetActive(false);
            BestNoticeText.gameObject.SetActive(false);
            AllCompleteText.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>Shows final counters, with flawless only for an unaided perfect attempt.</summary>
        public void Show(int mistakes, int undos)
        {
            Rating = null; coinEnabled=false;coinLine.gameObject.SetActive(false); Stars.gameObject.SetActive(false); MovesText.gameObject.SetActive(false);
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
            card.localScale = Vector3.one * Mathf.Min(1, size.x * 0.9f / 900, size.y * 0.9f / 1160);
            card.position = safeArea.TransformPoint(safeArea.rect.center);
        }
    }
}
