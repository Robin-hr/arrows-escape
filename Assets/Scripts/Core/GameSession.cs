using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Owns the shared board and applies moves immediately, independently of animation.</summary>
    public sealed class GameSession
    {
        private readonly Stack<MoveRecord> history = new Stack<MoveRecord>();
        // Preserve the Step 6 exit stack/count, including externally mutated test boards.
        // This additional stack orders exits and slides. Slides never touch history.
        private readonly Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();
        /// <summary>Exits minus successful undos in this attempt.</summary>
        public int MoveCount => history.Count;
        /// <summary>Number of arrows at the start of this attempt.</summary>
        public int StartingArrowCount { get; private set; }
        /// <summary>Total successful undos, never decremented.</summary>
        public int UndoCount { get; private set; }
        /// <summary>Accepted exit, slide and blocked taps; undo never subtracts.</summary>
        public int TapCount { get; private set; }
        /// <summary>Completed attempts cannot be undone.</summary>
        public bool CanUndo => moveHistory.Count > 0 && !IsCleared;

        /// <summary>Reverses the latest exit or slide; a failed board operation leaves both histories untouched.</summary>
        public UndoResult TryUndo()
        {
            if (IsCleared) return new UndoResult(UndoOutcome.LevelCleared, null, MoveCount);
            if (moveHistory.Count == 0) return new UndoResult(UndoOutcome.NothingToUndo, null, MoveCount);
            MoveRecord record=moveHistory.Peek(); Arrow previous=record.Before; Arrow restored;
            bool success=record.Kind==UndoMoveKind.Slide ?
                Board.TryMoveArrow(Board.GetArrowById(previous.Id),previous.X,previous.Y,out restored) :
                Board.TryAddArrow(previous,out restored);
            if (!success)
                return new UndoResult(UndoOutcome.Failed, null, MoveCount);
            moveHistory.Pop();
            if(record.Kind==UndoMoveKind.Exit) history.Pop();
            UndoCount++;
            return new UndoResult(UndoOutcome.Undone, restored, MoveCount,record.Kind);
        }

        /// <summary>The same live board used by the view and tap tracker.</summary>
        public Board Board { get; private set; }
        /// <summary>The number of blocked taps; mistakes never end a level.</summary>
        public int MistakeCount { get; private set; }
        /// <summary>Whether the current board has no remaining arrows.</summary>
        public bool IsCleared => Board.IsCleared();

        /// <summary>Starts a session with a non-null board and no mistakes.</summary>
        public GameSession(Board board) => Reset(board);

        /// <summary>Exits a free arrow, slides blocked ice across a gap, counts blocked taps, or ignores stale instances.</summary>
        public MoveResult TryMove(Arrow arrow)
        {
            if (arrow == null || IsCleared || !ReferenceEquals(Board.GetArrowById(arrow.Id), arrow))
                return new MoveResult(MoveOutcome.Ignored, arrow, null, false, MistakeCount);
            TapCount++;
            if (Board.CanExit(arrow))
            {
                Board.Remove(arrow);
                var record=new MoveRecord(arrow); history.Push(record); moveHistory.Push(record);
                return new MoveResult(MoveOutcome.Exited, arrow, null, Board.IsCleared(), MistakeCount);
            }
            if(Board.CanSlide(arrow))
            {
                int cells=Board.CountFreeCellsAhead(arrow);
                if(Board.TryMoveArrow(arrow,arrow.X+arrow.Direction.GetDx()*cells,arrow.Y+arrow.Direction.GetDy()*cells,out Arrow moved))
                { moveHistory.Push(MoveRecord.ForSlide(arrow,moved)); return new MoveResult(arrow,moved,cells,MistakeCount); }
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
            moveHistory.Clear();
            UndoCount = 0;
            TapCount = 0;
            StartingArrowCount = board.Arrows.Count;
            MistakeCount = 0;
        }
    }
}

