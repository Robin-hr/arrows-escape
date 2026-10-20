using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Owns the shared board and applies moves immediately, independently of animation.</summary>
    public sealed class GameSession
    {
        private readonly Stack<MoveRecord> history = new Stack<MoveRecord>();
        /// <summary>Exits minus successful undos in this attempt.</summary>
        public int MoveCount => history.Count;
        /// <summary>Number of arrows at the start of this attempt.</summary>
        public int StartingArrowCount { get; private set; }
        /// <summary>Total successful undos, never decremented.</summary>
        public int UndoCount { get; private set; }
        /// <summary>Completed attempts cannot be undone.</summary>
        public bool CanUndo => history.Count > 0 && !IsCleared;

        /// <summary>Restores the latest exit; a failed add leaves history untouched.</summary>
        public UndoResult TryUndo()
        {
            if (IsCleared) return new UndoResult(UndoOutcome.LevelCleared, null, MoveCount);
            if (history.Count == 0) return new UndoResult(UndoOutcome.NothingToUndo, null, MoveCount);
            Arrow previous = history.Peek().Arrow;
            if (!Board.TryAddArrow(previous.Id, previous.X, previous.Y, previous.Direction, out Arrow restored))
                return new UndoResult(UndoOutcome.Failed, null, MoveCount);
            history.Pop();
            UndoCount++;
            return new UndoResult(UndoOutcome.Undone, restored, MoveCount);
        }

        /// <summary>The same live board used by the view and tap tracker.</summary>
        public Board Board { get; private set; }
        /// <summary>The number of blocked taps; mistakes never end a level.</summary>
        public int MistakeCount { get; private set; }
        /// <summary>Whether the current board has no remaining arrows.</summary>
        public bool IsCleared => Board.IsCleared();

        /// <summary>Starts a session with a non-null board and no mistakes.</summary>
        public GameSession(Board board) => Reset(board);

        /// <summary>Removes a free arrow immediately, counts a blocked tap, or ignores an invalid request.</summary>
        public MoveResult TryMove(Arrow arrow)
        {
            if (arrow == null || IsCleared || !ReferenceEquals(Board.GetArrowById(arrow.Id), arrow))
                return new MoveResult(MoveOutcome.Ignored, arrow, null, false, MistakeCount);
            if (Board.CanExit(arrow))
            {
                Board.Remove(arrow);
                history.Push(new MoveRecord(arrow));
                return new MoveResult(MoveOutcome.Exited, arrow, null, Board.IsCleared(), MistakeCount);
            }
            MistakeCount++;
            return new MoveResult(MoveOutcome.Blocked, arrow, Board.GetBlocker(arrow), false, MistakeCount);
        }

        /// <summary>Swaps the board and clears mistakes; null leaves the existing session unchanged.</summary>
        public void Reset(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            Board = board;
            history.Clear();
            UndoCount = 0;
            StartingArrowCount = board.Arrows.Count;
            MistakeCount = 0;
        }
    }
}
