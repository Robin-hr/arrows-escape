using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Builds runtime uGUI and connects session events, buttons, and board pointer gating.</summary>
    public sealed class HudController : MonoBehaviour
    {
        private GameController game;
        private GameBootstrap bootstrap;
        private PointerInputAdapter input;
        private Canvas canvas;
        private System.Func<Vector2, bool> blockHandler;
        private bool bound;
        private bool visible = true;
        /// <summary>Whether the HUD canvas is enabled by screen flow.</summary>
        public bool IsVisible => visible;
        /// <summary>Raised by the safe-area pause button.</summary>
        public event System.Action PauseRequested;
        /// <summary>Hides both HUD and completion; restoring pulls the latest logical state.</summary>
        public void SetVisible(bool value)
        {
            visible = value;
            if (canvas == null) return;
            canvas.gameObject.SetActive(value);
            if (!value) CompletePanel.Hide();
            else { Refresh(); if (game != null && game.IsCleared) OnCleared(); else CompletePanel.Hide(); }
        }
        /// <summary>The current HUD display.</summary>
        public HudView Hud { get; private set; }
        /// <summary>The final completion overlay.</summary>
        public LevelCompletePanel CompletePanel { get; private set; }

        private void Start()
        {
            if (bound) return;
            var controller = Object.FindFirstObjectByType<GameController>();
            var levels = Object.FindFirstObjectByType<GameBootstrap>();
            if (controller == null || levels == null)
            {
                Debug.LogWarning("ArrowEscape UI: GameController and GameBootstrap are required.", this);
                enabled = false;
                return;
            }
            EventSystemEnsurer.Ensure();
            Bind(controller, levels);
        }

        /// <summary>Binds explicit controllers; tests can drive buttons without an EventSystem.</summary>
        public void Bind(GameController controller, GameBootstrap levels)
        {
            Unbind();
            Build();
            game = controller;
            bootstrap = levels;
            if (game == null) return;
            bound = true;
            canvas.gameObject.SetActive(visible);
            game.MoveCountChanged += Refresh;
            game.UndoStateChanged += Refresh;
            game.MistakeCountChanged += OnMistakes;
            game.LevelStarted += OnLevelStarted;
            game.BoardCleared += OnCleared;
            if (bootstrap != null) bootstrap.LevelCompleted += OnLevelCompleted;
            input = game.GetComponent<PointerInputAdapter>();
            blockHandler = IsBlocked;
            if (input != null) input.IsScreenPointBlocked = blockHandler;
            Refresh();
            if (visible && game.IsCleared) OnCleared(); else CompletePanel.Hide();
        }

        /// <summary>Blocks whole modal interactions and the padded bottom button rectangles.</summary>
        public bool IsBlocked(Vector2 screenPosition)
        {
            if (CompletePanel == null) return false;
            if (CompletePanel.IsVisible) return true;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return Contains(Hud.PauseButton.transform as RectTransform, screenPosition, camera) || Contains(Hud.UndoButton.transform as RectTransform, screenPosition, camera) ||
                Contains(Hud.HintButton.transform as RectTransform, screenPosition, camera) ||
                Contains(Hud.RestartButton.transform as RectTransform, screenPosition, camera);
        }

        private static bool Contains(RectTransform rect, Vector2 point, Camera camera)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, point, camera)) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, point, camera, out Vector2 local)) return false;
            Rect padded = rect.rect;
            padded.xMin -= ViewTheme.UiBlockPadding; padded.xMax += ViewTheme.UiBlockPadding;
            padded.yMin -= ViewTheme.UiBlockPadding; padded.yMax += ViewTheme.UiBlockPadding;
            return padded.Contains(local);
        }

        private void Build()
        {
            if (canvas != null) return;
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform safe = UiFactory.Rect("SafeArea", root.transform, Vector2.zero, Vector2.zero);
            UiFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaPanel>().Apply();
            Hud = gameObject.AddComponent<HudView>();
            Hud.Build(safe);
            CompletePanel = gameObject.AddComponent<LevelCompletePanel>();
            CompletePanel.Build(root.GetComponent<RectTransform>(), safe);
            Hud.UndoButton.onClick.AddListener(Undo);
            Hud.RestartButton.onClick.AddListener(Restart);
            Hud.HintButton.onClick.AddListener(Hint);
            Hud.PauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
            CompletePanel.ReplayButton.onClick.AddListener(Replay);
            CompletePanel.NextButton.onClick.AddListener(Next);
        }

        private void Undo() => game.Undo();
        private void Restart() => game.RestartLevel();
        private void Hint() => game.ShowHint();
        private void Replay()
        {
            if (!ConsumeCompletion()) return;
            if (bootstrap != null) bootstrap.RestartLevel();
            else game.RestartLevel();
        }

        private void Next()
        {
            if (bootstrap != null && bootstrap.CanGoNext && ConsumeCompletion()) bootstrap.NextLevel();
        }

        private bool ConsumeCompletion()
        {
            if (game == null || !game.IsCleared || !CompletePanel.IsVisible) return false;
            // Consume this completion before loading: duplicate callbacks cannot skip a level.
            CompletePanel.Hide();
            return true;
        }
        private void OnMistakes(int count) => Refresh();
        private void OnLevelStarted() { CompletePanel.Hide(); Refresh(); }
        private void OnCleared()
        {
            if (!visible) return;
            Refresh();
            if (bootstrap != null && bootstrap.LastCompletion != null) OnLevelCompleted(bootstrap.LastCompletion);
            else CompletePanel.Show(game.MistakeCount, game.UndoCount);
        }
        private void OnLevelCompleted(CompletionResult result)
        {
            if (!visible) return;
            CompletePanel.Show(game.MistakeCount, game.UndoCount, result, bootstrap.IsLastLevel,
                bootstrap.AllLevelsCompleted, bootstrap.CanGoNext);
        }
        private void Refresh()
        {
            Hud.SetState(game.LevelName, game.MoveCount, game.StartingArrowCount, game.MistakeCount, game.CanUndo);
            Hud.SetHintState(game.CanHint);
            if (bootstrap != null) Hud.SetLevelPosition(bootstrap.CurrentLevelIndex, bootstrap.LevelCount);
        }

        private void Unbind()
        {
            if (game != null)
            {
                game.MoveCountChanged -= Refresh;
                game.UndoStateChanged -= Refresh;
                game.MistakeCountChanged -= OnMistakes;
                game.LevelStarted -= OnLevelStarted;
                game.BoardCleared -= OnCleared;
            }
            if (input != null && input.IsScreenPointBlocked == blockHandler) input.IsScreenPointBlocked = null;
            if (bootstrap != null) bootstrap.LevelCompleted -= OnLevelCompleted;
            bound = false;
        }
        private void OnDestroy() => Unbind();
    }
}
