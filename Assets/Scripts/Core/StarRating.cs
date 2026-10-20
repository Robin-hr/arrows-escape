using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure move budgets and completion stars, independent of exit progress.</summary>
    public static class StarRating
    {
        /// <summary>Uses arrow count for ordinary boards, and a bounded solver path for ice.</summary>
        public static int Par(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            int count = board.Arrows.Count;
            // Empty diagnostic boards still need a valid HUD budget; playable levels have arrows.
            if (!board.HasIceArrows) return Math.Max(1, count);
            SolveResult solution = PuzzleSolver.Analyze(board);
            if (solution.Status == SolveStatus.Solved) return Math.Max(1, solution.Path.Count);
            foreach (Arrow arrow in board.Arrows) if (arrow.Kind == ArrowKind.Ice) count++;
            return Math.Max(1, count);
        }
        /// <summary>Par plus one third rounded up, saturated at the integer limit.</summary>
        public static int TwoStarLimit(int par)
        {
            if (par < 1) throw new ArgumentOutOfRangeException(nameof(par));
            return (int)Math.Min(int.MaxValue, (long)par + ((long)par + 2) / 3);
        }
        /// <summary>Every completed attempt earns at least one star.</summary>
        public static int Stars(int moves, int par)
        {
            if (moves < 0) throw new ArgumentOutOfRangeException(nameof(moves));
            int limit = TwoStarLimit(par);
            return moves <= par ? 3 : moves <= limit ? 2 : 1;
        }
        /// <summary>Potential stars at the current tap count, for the live meter.</summary>
        public static int StarsIfClearedNow(int moves, int par) => Stars(moves, par);
    }
}
