using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Applies instant session moves and schedules independent visual feedback.</summary>
    [RequireComponent(typeof(BoardView), typeof(TapController), typeof(ArrowAnimator))]
    public sealed class GameController : MonoBehaviour
    {
        private BoardView boardView;
        private TapController tapController;
        private ArrowAnimator animator;
        private string resourcePath;
        private Board initialBoard;
        /// <summary>Optional feedback controller; plain gameplay tests need no guide component.</summary>
        public GuideController Guide { get; private set; }
        /// <summary>Requests accepted in this attempt; never part of saved results.</summary>
        public int HintCount { get; private set; }
        /// <summary>Whether this attempt permits requesting a hint.</summary>
        public bool CanHint => !IsPaused && Session != null && !IsCleared && Session.Board.Arrows.Count > 0;
        /// <summary>Raised with the chosen free arrow, without changing the session.</summary>
        public event Action<Arrow> HintShown;
        /// <summary>Wires an optional guide to the same board display and pause flag.</summary>
        public void BindGuide(GuideController value)
        {
            if (Guide != null && Guide != value) Guide.ClearAll();
            Guide = value;
            if (Guide != null) { Guide.Bind(GetComponent<BoardView>()); Guide.SetPaused(IsPaused); }
        }
        /// <summary>Shows one deterministic free-arrow suggestion; never changes any gameplay counter.</summary>
        public bool ShowHint()
        {
            if (!CanHint) return false;
            HintResult result = HintFinder.Find(Session.Board);
            if (!result.HasHint) return false;
            if (Guide != null) Guide.ShowHint(result.Arrow);
            HintCount++;
            HintShown?.Invoke(result.Arrow);
            return true;
        }
        /// <summary>Whether gameplay input and undo are paused.</summary>
        public bool IsPaused { get; private set; }
        /// <summary>Pauses input and automatic visuals, clearing any active press.</summary>
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (animator == null) animator = GetComponent<ArrowAnimator>();
            animator.SetPaused(paused);
            if (Guide != null) Guide.SetPaused(paused);
            GetComponent<TapController>().ResetInteraction();
        }
        /// <summary>Discards a mid-attempt board without recording a result.</summary>
        public void DiscardAttempt()
        {
            SetPaused(true);
            animator.StopAll();
            GetComponent<BoardView>().Clear();
            Session = null;
            initialBoard = null;
            resourcePath = null;
        }

        /// <summary>The logical owner of the same board displayed and used for taps.</summary>
        public GameSession Session { get; private set; }
        /// <summary>Current blocked-tap count; mistakes never fail the level.</summary>
        public int MistakeCount => Session == null ? 0 : Session.MistakeCount;
        /// <summary>Whether the active session has no arrows remaining.</summary>
        public bool IsCleared => Session != null && Session.IsCleared;
        /// <summary>The current display name for the development HUD.</summary>
        public string LevelName { get; private set; } = "Level";

        /// <summary>Raised after an arrow is logically removed and scheduled to slide.</summary>
        public event Action<Arrow> ArrowExited;
        /// <summary>Raised for a blocked arrow and its nearest blocker.</summary>
        public event Action<Arrow, Arrow> MoveBlocked;
        /// <summary>Raised after a blocked tap, or with zero when a level starts.</summary>
        public event Action<int> MistakeCountChanged;
        /// <summary>Raised once when a move removes the final arrow.</summary>
        public event Action BoardCleared;
        /// <summary>Raised after progress changes.</summary>
        public event Action MoveCountChanged;
        /// <summary>Raised whenever undo availability may change.</summary>
        public event Action UndoStateChanged;
        /// <summary>Raised with the new restored arrow instance.</summary>
        public event Action<Arrow> ArrowRestored;
        /// <summary>Raised after the new board has been shown.</summary>
        public event Action LevelStarted;
        /// <summary>Current removed-arrow count.</summary>
        public int MoveCount => Session == null ? 0 : Session.MoveCount;
        /// <summary>Arrow count when the attempt started.</summary>
        public int StartingArrowCount => Session == null ? 0 : Session.StartingArrowCount;
        /// <summary>Total successful undos this attempt.</summary>
        public int UndoCount => Session == null ? 0 : Session.UndoCount;
        /// <summary>Whether the latest exit can be restored.</summary>
        public bool CanUndo => Session != null && Session.CanUndo;

        /// <summary>Restores one exit and replaces any still-sliding display.</summary>
        public void Undo()
        {
            if (Session == null || IsPaused) return;
            UndoResult result = Session.TryUndo();
            if (result.Outcome != UndoOutcome.Undone) return;
            if (Guide != null) Guide.ClearAll();
            if (boardView.TryGetReleasedView(result.Restored.Id, out ArrowView released)) animator.Cancel(released);
            boardView.DiscardReleasedView(result.Restored.Id);
            boardView.AddArrowView(result.Restored);
            tapController.ResetInteraction();
            MoveCountChanged?.Invoke();
            UndoStateChanged?.Invoke();
            ArrowRestored?.Invoke(result.Restored);
        }

        /// <summary>Starts with the supplied live board, stopping and removing old visuals.</summary>
        public void StartLevel(Board board) => StartLevel(board, LevelName);

        /// <summary>Starts a named attempt using the same board for session, view, and taps.</summary>
        public void StartLevel(Board board, string levelName)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            SetPaused(false);
            LevelName = levelName ?? "Level";
            initialBoard = board.Clone();
            EnsureBindings();
            animator.StopAll();
            if (Guide != null) Guide.ClearAll();
            HintCount = 0;
            if (Session == null) Session = new GameSession(board);
            else Session.Reset(board);
            boardView.Show(board);
            MistakeCountChanged?.Invoke(0);
            MoveCountChanged?.Invoke();
            UndoStateChanged?.Invoke();
            LevelStarted?.Invoke();
        }

        /// <summary>Sets the loaded level's name and Resources path for restart.</summary>
        public void SetLevelInfo(string name, string path)
        {
            LevelName = name ?? "Level";
            resourcePath = path;
        }

        /// <summary>Reloads the current JSON resource into a fresh session and board.</summary>
        [ContextMenu("Restart Level")]
        public void RestartLevel()
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                if (initialBoard != null) StartLevel(initialBoard.Clone(), LevelName);
                return;
            }
            LevelLoadResult result = LevelRepository.LoadFromResources(resourcePath);
            if (result.Success) StartLevel(result.Board);
            else foreach (string error in result.Errors) Debug.LogError(error, this);
        }

        private void EnsureBindings()
        {
            boardView = GetComponent<BoardView>();
            animator = GetComponent<ArrowAnimator>();
            BindGuide(GetComponent<GuideController>());
            if (tapController != null) tapController.ArrowTapped -= OnTapped;
            tapController = GetComponent<TapController>();
            tapController.Bind(boardView);
            tapController.ArrowTapped += OnTapped;
        }

        private void OnTapped(Arrow arrow)
        {
            if (Session == null || IsPaused) return;
            MoveResult result = Session.TryMove(arrow);
            if (result.Outcome == MoveOutcome.Exited)
            {
                if (Guide != null) Guide.ClearAll();
                if (boardView.ReleaseArrowView(arrow.Id, out ArrowView view))
                    animator.PlaySlideOut(view, arrow.Direction, Session.Board.Width, Session.Board.Height, arrow.X, arrow.Y);
                ArrowExited?.Invoke(arrow);
                MoveCountChanged?.Invoke();
                UndoStateChanged?.Invoke();
            }
            else if (result.Outcome == MoveOutcome.Blocked)
            {
                boardView.TryGetArrowView(arrow.Id, out ArrowView tapped);
                boardView.TryGetArrowView(result.Blocker.Id, out ArrowView blocker);
                animator.PlayBlocked(tapped, arrow.Direction, blocker);
                if (Guide != null) Guide.ShowBlockerLine(arrow, result.Blocker);
                MoveBlocked?.Invoke(arrow, result.Blocker);
                MistakeCountChanged?.Invoke(result.MistakeCount);
            }
            if (!result.BoardCleared) return;
            BoardCleared?.Invoke();
            Debug.Log($"Level cleared. Mistakes: {result.MistakeCount}", this);
        }

        private void OnEnable() => EnsureBindings();

        private void OnDisable()
        {
            if (tapController != null) tapController.ArrowTapped -= OnTapped;
            if (animator != null) animator.StopAll();
            if (Guide != null) Guide.ClearAll();
        }
    }
}
