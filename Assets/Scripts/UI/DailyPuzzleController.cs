using System;
using System.Threading.Tasks;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>
    /// Three hearts on every level and the daily puzzle (a mistake costs one; at zero: watch an ad or buy a heart
    /// to continue, restart, or go home), plus the daily puzzle's streak result panel and share text.
    /// </summary>
    public sealed class DailyPuzzleController : MonoBehaviour
    {
        private GameBootstrap bootstrap;
        private GameController game;
        private HudController hud;
        private Action home;
        private Func<int> today;
        private ToastView toast;
        private Task<Board> building;
        private int buildingKey, lastMistakes;
        private Action<Board, int> onBuilt;
        /// <summary>Out-of-hearts panel.</summary>
        public DailyFailView Fail { get; private set; }
        /// <summary>Result panel.</summary>
        public DailyResultView Result { get; private set; }
        /// <summary>Hearts left in the current attempt.</summary>
        public int Hearts { get; private set; } = EconomyConfig.DailyHearts;
        /// <summary>The last text copied by Share.</summary>
        public string LastShareText { get; private set; }
        /// <summary>True while a daily panel covers the board.</summary>
        public bool IsModalOpen => (Fail != null && Fail.IsOpen) || (Result != null && Result.IsOpen);
        /// <summary>True while today's board is being generated on a worker thread.</summary>
        public bool IsBuilding => building != null;
        /// <summary>Raised when the result panel opens.</summary>
        public event Action Completed;

        /// <summary>Builds the panels and subscribes to the game.</summary>
        public void Initialize(Transform canvas, GameBootstrap boot, GameController controller, HudController display, ToastView notices, Func<int> todayKey, Action goHome)
        {
            bootstrap = boot; game = controller; hud = display; toast = notices; today = todayKey; home = goHome;
            Fail = gameObject.AddComponent<DailyFailView>(); Fail.Build(canvas);
            Result = gameObject.AddComponent<DailyResultView>(); Result.Build(canvas);
            Fail.RestartButton.onClick.AddListener(RestartDaily);
            Fail.AdButton.onClick.AddListener(WatchAdToContinue);
            Fail.BuyButton.onClick.AddListener(BuyHeart);
            Fail.HomeButton.onClick.AddListener(() => { Fail.Hide(); home?.Invoke(); });
            Result.ShareButton.onClick.AddListener(Share);
            Result.HomeButton.onClick.AddListener(() => { Result.Hide(); home?.Invoke(); });
            game.LevelStarted += OnLevelStarted;
            game.MistakeCountChanged += OnMistakes;
            game.BoardCleared += OnCleared;
            bootstrap.DailyCompleted += OnDailyCompleted;
        }

        /// <summary>Generates the board for a date (worker thread in Play mode, inline in Edit Mode), then calls back.</summary>
        public void Build(int dateKey, Action<Board, int> built)
        {
            if (building != null) return;
            onBuilt = built; buildingKey = dateKey;
            if (!Application.isPlaying) { built?.Invoke(DailyPuzzleBuilder.Build(dateKey), dateKey); return; }
            building = Task.Run(() => DailyPuzzleBuilder.Build(dateKey));
        }

        private void Update()
        {
            if (building == null || !building.IsCompleted) return;
            Board board = building.Status == TaskStatus.RanToCompletion ? building.Result : null; building = null;
            if (board == null) { Debug.LogWarning("ArrowEscape daily puzzle could not be generated."); return; }
            onBuilt?.Invoke(board, buildingKey);
        }

        private void OnLevelStarted()
        {
            Fail.Hide(); Result.Hide();
            Hearts = EconomyConfig.DailyHearts; lastMistakes = game.MistakeCount;
            hud.Hud.SetHearts(Hearts);
        }

        private void OnMistakes(int count)
        {
            if (count < lastMistakes) { lastMistakes = count; return; }
            if (count > lastMistakes) Hearts = Math.Max(0, Hearts - (count - lastMistakes));
            lastMistakes = count; hud.Hud.SetHearts(Hearts);
            if (Hearts <= 0 && !game.IsCleared)
            {
                Fail.Show(bootstrap.Inventory.State.Coins);
                var audio = game.GetComponent<AudioDirector>(); if (audio != null) audio.PlayLose();   // sad trombone (Step 14M)
            }
        }

        /// <summary>Starts the puzzle again with full hearts.</summary>
        public void RestartDaily() { if (BeforeRestart != null && !BeforeRestart()) return; Fail.Hide(); bootstrap.RestartLevel(); }
        /// <summary>Asked before RESTART starts a new attempt (Journey spends a life); false cancels.</summary>
        public Func<bool> BeforeRestart { get; set; }

        private void OnCleared() => Fail.Hide();

        /// <summary>Buys one heart for coins and continues.</summary>
        public void BuyHeart()
        {
            if (!bootstrap.Inventory.Shop.TrySpendCoins(EconomyConfig.HeartPrice)) return;
            game.RequestUiFeedback(UiFeedbackCue.Purchase);
            bootstrap.Inventory.Save(); Hearts = 1; hud.Hud.SetHearts(Hearts); Fail.Hide();
        }

        /// <summary>Watches a rewarded ad and continues with one heart.</summary>
        public void WatchAdToContinue()
        {
            void Continue(bool earned) { if (!earned) return; Hearts = 1; hud.Hud.SetHearts(Hearts); Fail.Hide(); }
            if (RewardedAds.Service == null) Continue(true); else RewardedAds.Service.Show("daily_continue", Continue);
        }

        private void OnDailyCompleted()
        {
            int streak = bootstrap.LastDailyStreak, key = bootstrap.DailyKey;
            int badge = bootstrap.LastDailyFirstSolve && Array.IndexOf(DailyStreakRules.Badges, streak) >= 0 ? streak : 0;
            LastShareText = DailyStreakRules.ShareText(key, game.TapCount, game.MistakeCount, Hearts, streak);
            Result.Show(key, game.TapCount, game.MistakeCount, Hearts, streak, badge, DailyStreakRules.NextBadge(bootstrap.Inventory.State.DailyBestStreak));
            Completed?.Invoke();
        }

        /// <summary>Copies the short result (moves, mistakes, date, streak) to the clipboard and opens the phone share sheet.</summary>
        public void Share()
        {
            if (string.IsNullOrEmpty(LastShareText)) return;
            // Always copy (works everywhere); phones also open their native share sheet (Step 14N).
            try { GUIUtility.systemCopyBuffer = LastShareText; } catch (Exception e) { Debug.LogWarning("Clipboard unavailable: " + e.Message); }
            if (!NativeShare.Share(LastShareText, "Share your Arrows Escape result"))
                toast?.Show("RESULT COPIED!\nPaste it anywhere to share", "Daily/icon_share");
        }

        private void OnDestroy()
        {
            if (game != null) { game.LevelStarted -= OnLevelStarted; game.MistakeCountChanged -= OnMistakes; game.BoardCleared -= OnCleared; }
            if (bootstrap != null) bootstrap.DailyCompleted -= OnDailyCompleted;
        }
    }
}
