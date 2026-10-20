using System;

namespace ArrowEscape.Core
{
    /// <summary>Tracks Idle/Pressing states for one arrow and one pointer, without game rules.</summary>
    public sealed class TapTracker
    {
        private Board board;
        private readonly TapSettings settings;
        private int pointerId;

        /// <summary>Whether an arrow is currently being pressed; otherwise the state is Idle.</summary>
        public bool IsPressing => PressedArrow != null;
        /// <summary>The original pressed arrow, or null while Idle.</summary>
        public Arrow PressedArrow { get; private set; }
        /// <summary>Whether the active pointer remains inside the arrow's cancellation radius.</summary>
        public bool IsStillOver { get; private set; }

        // Events report transitions; completion clears state before notifying callers.
        /// <summary>Raised once when an arrow is selected.</summary>
        public event Action<Arrow> Pressed;
        /// <summary>Raised when a press is cancelled, including reset or board replacement.</summary>
        public event Action<Arrow> Cancelled;
        /// <summary>Raised once for a successful release on the originally pressed arrow.</summary>
        public event Action<Arrow> Tapped;

        /// <summary>Creates a tracker; a null board is allowed and cannot select anything.</summary>
        public TapTracker(Board board, TapSettings settings)
        {
            this.board = board;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Cancels the current press before switching boards.</summary>
        public void SetBoard(Board value)
        {
            Reset();
            board = value;
        }

        /// <summary>Selects the nearest arrow only when Idle.</summary>
        public void PointerDown(int id, float cellX, float cellY)
        {
            if (IsPressing) return;
            Arrow arrow = TapResolver.Resolve(board, cellX, cellY, settings.SelectRadius);
            if (arrow == null) return;
            pointerId = id;
            PressedArrow = arrow;
            IsStillOver = true;
            Pressed?.Invoke(arrow);
        }

        /// <summary>Updates distance from the original arrow, ignoring other pointers.</summary>
        public void PointerMove(int id, float cellX, float cellY)
        {
            if (Owns(id)) IsStillOver = WithinCancelRadius(cellX, cellY);
        }

        /// <summary>Taps or cancels the original arrow, then returns to Idle.</summary>
        public void PointerUp(int id, float cellX, float cellY)
        {
            if (!Owns(id)) return;
            Arrow arrow = PressedArrow;
            bool tapped = board != null && ReferenceEquals(board.GetArrowById(arrow.Id), arrow)
                && WithinCancelRadius(cellX, cellY);
            ClearState();
            if (tapped) Tapped?.Invoke(arrow);
            else Cancelled?.Invoke(arrow);
        }

        /// <summary>Cancels only the owning pointer's press, never reporting a tap.</summary>
        public void PointerCancel(int id)
        {
            if (Owns(id)) Reset();
        }

        /// <summary>Returns to Idle, reporting cancellation if an arrow was pressed.</summary>
        public void Reset()
        {
            Arrow arrow = PressedArrow;
            ClearState();
            if (arrow != null) Cancelled?.Invoke(arrow);
        }

        private bool Owns(int id) => IsPressing && pointerId == id;

        private bool WithinCancelRadius(float x, float y)
        {
            for(int i=0;i<PressedArrow.Length;i++)
            {
                double dx=(double)x-PressedArrow.Cells[i].X,dy=(double)y-PressedArrow.Cells[i].Y;
                if(dx*dx+dy*dy <= (double)settings.CancelRadius*settings.CancelRadius)return true;
            }
            return false;
        }

        private void ClearState()
        {
            PressedArrow = null;
            IsStillOver = false;
            pointerId = 0;
        }
    }
}
