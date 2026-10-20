using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Connects world-space presses to core tracking, highlights, and tap reports.</summary>
    public sealed class TapController : MonoBehaviour
    {
        private BoardView boardView;
        private TapTracker tracker;

        /// <summary>One successful tap, delivered after the press highlight has been cleared.</summary>
        public event Action<Arrow> ArrowTapped;

        /// <summary>Cancels the previous interaction and binds to a board display.</summary>
        public void Bind(BoardView view)
        {
            tracker?.Reset();
            if (boardView != null) boardView.BoardChanged -= OnBoardChanged;
            boardView = view;
            tracker = null;
            if (boardView == null) return;
            boardView.TryGetBoard(out Board board);
            tracker = new TapTracker(board, new TapSettings());
            tracker.Pressed += OnPressed;
            tracker.Cancelled += OnCancelled;
            tracker.Tapped += OnTapped;
            boardView.BoardChanged += OnBoardChanged;
        }

        /// <summary>Starts a press using a world position; no camera is required for injected input.</summary>
        public void PointerDown(int pointerId, Vector2 worldPosition)
        {
            if (boardView == null || tracker == null) return;
            Vector2 cell = boardView.WorldToCell(worldPosition);
            tracker.PointerDown(pointerId, cell.x, cell.y);
        }

        /// <summary>Updates the original arrow's still-over highlight.</summary>
        public void PointerMove(int pointerId, Vector2 worldPosition)
        {
            if (boardView == null || tracker == null) return;
            Vector2 cell = boardView.WorldToCell(worldPosition);
            tracker.PointerMove(pointerId, cell.x, cell.y);
            if (tracker.IsPressing) Highlight(tracker.PressedArrow, true, tracker.IsStillOver);
        }

        /// <summary>Reports a successful release or clears a cancelled press.</summary>
        public void PointerUp(int pointerId, Vector2 worldPosition)
        {
            if (boardView == null || tracker == null) return;
            Vector2 cell = boardView.WorldToCell(worldPosition);
            tracker.PointerUp(pointerId, cell.x, cell.y);
        }

        /// <summary>Cancels the owning pointer without logging or reporting a tap.</summary>
        public void PointerCancel(int pointerId) => tracker?.PointerCancel(pointerId);

        /// <summary>Cancels the current press and restores its highlight.</summary>
        public void ResetInteraction() => tracker?.Reset();

        private void OnBoardChanged()
        {
            boardView.TryGetBoard(out Board board);
            tracker.SetBoard(board);
        }

        private void OnPressed(Arrow arrow) => Highlight(arrow, true, true);
        private void OnCancelled(Arrow arrow) => Highlight(arrow, false, true);

        private void OnTapped(Arrow arrow)
        {
            Highlight(arrow, false, true);
            ArrowTapped?.Invoke(arrow);
        }

        private void Highlight(Arrow arrow, bool on, bool stillOver)
        {
            if (boardView != null && boardView.TryGetArrowView(arrow.Id, out ArrowView view) && view != null)
                view.SetHighlight(on, stillOver);
        }

        private void OnDisable() => tracker?.Reset();
        private void OnDestroy() => Bind(null);
    }
}
