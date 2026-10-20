using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Selects a useful free arrow on normal boards, or a solution action on ice boards.</summary>
    public static class HintFinder
    {
        /// <summary>Never mutates the board. Normal boards use path scores; ice boards use bounded search.</summary>
        public static HintResult Find(Board board)
        {
            if (board == null || board.IsCleared()) return HintResult.None;
            if(board.HasIceArrows)
            {
                SolveResult result=PuzzleSolver.Analyze(board);
                if(result.Status!=SolveStatus.Solved || result.Path.Count==0)return HintResult.None;
                SolverMove move=result.Path[0];return new HintResult(board.GetArrowById(move.ArrowId),move.IsSlide);
            }
            ScorePaths(board, out int[] freed, out int[] dependents, out bool[] free);
            int best = -1;
            for (int i = 0; i < board.Arrows.Count; i++)
            {
                if (!free[i]) continue;
                if (best < 0 || freed[i] > freed[best] ||
                    (freed[i] == freed[best] && (dependents[i] > dependents[best] ||
                    (dependents[i] == dependents[best] && board.Arrows[i].Id < board.Arrows[best].Id)))) best = i;
            }
            // Removing a free arrow never makes this puzzle harder. Revisit this rule for special arrow types.
            return best < 0 ? HintResult.None : new HintResult(true, board.Arrows[best], freed[best], dependents[best]);
        }

        private static void ScorePaths(Board board, out int[] newlyFreed, out int[] dependents, out bool[] free)
        {
            int count = board.Arrows.Count;
            newlyFreed = new int[count]; dependents = new int[count]; free = new bool[count];
            var indices = new Dictionary<int, int>(count);
            for (int i = 0; i < count; i++) indices.Add(board.Arrows[i].Id, i);
            for (int i = 0; i < count; i++)
            {
                Arrow arrow = board.Arrows[i];
                int dx = arrow.Direction.GetDx(), dy = arrow.Direction.GetDy();
                int blockers = 0, soleBlocker = -1;
                var seen = new HashSet<int>();
                for (int x = arrow.X + dx, y = arrow.Y + dy; board.InBounds(x, y); x += dx, y += dy)
                {
                    Arrow blocker = board.GetArrowAt(x, y);
                    if (blocker == null || !seen.Add(blocker.Id)) continue;
                    soleBlocker = indices[blocker.Id];
                    dependents[soleBlocker]++;
                    blockers++;
                }
                free[i] = blockers == 0; // The same path-clear condition as Board.CanExit, without a second walk.
                if (blockers == 1) newlyFreed[soleBlocker]++;
            }
        }
    }
}

