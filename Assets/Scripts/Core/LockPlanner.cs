using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Step 15: adds padlock/key pairs to a solvable all-Normal board. A padlocked arrow cannot move until its key
    /// arrow has left, which turns "find any free arrow" into real planning: the generator padlocks arrows that are
    /// free early and that block many others, and gives them keys buried deep in long blocking chains. Every pair keeps
    /// the dependency graph acyclic, so the board stays solvable.
    /// </summary>
    public static class LockPlanner
    {
        /// <summary>Returns a copy of <paramref name="board"/> with up to <paramref name="pairs"/> padlocks, never fewer than <paramref name="minFree"/> free arrows.</summary>
        public static Board AddLocks(Board board, int pairs, int seed, int minFree = 2)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.HasIceArrows) throw new ArgumentException("Padlocks are only planned on all-Normal boards.");
            var arrows = new List<Arrow>(board.Arrows); arrows.Sort((a, b) => a.Id.CompareTo(b.Id));
            int n = arrows.Count; if (pairs <= 0 || n < 6) return board.Clone();
            var index = new Dictionary<int, int>(); for (int i = 0; i < n; i++) index[arrows[i].Id] = i;
            // deps[i]: arrows that must leave before i (everything on i's ray).
            var deps = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                deps[i] = new List<int>(); var seen = new HashSet<int>(); Arrow a = arrows[i]; int dx = a.Direction.GetDx(), dy = a.Direction.GetDy();
                for (int x = a.X + dx, y = a.Y + dy; board.InBounds(x, y); x += dx, y += dy)
                {
                    Arrow o = board.GetArrowAt(x, y); if (o != null && o.Id != a.Id && seen.Add(o.Id)) deps[i].Add(index[o.Id]);
                }
                if (a.LockedBy != 0 && index.TryGetValue(a.LockedBy, out int k) && seen.Add(a.LockedBy)) deps[i].Add(k);
            }
            var lockOf = new int[n]; for (int i = 0; i < n; i++) lockOf[i] = arrows[i].LockedBy == 0 ? -1 : index[arrows[i].LockedBy];
            var used = new bool[n]; for (int i = 0; i < n; i++) if (lockOf[i] >= 0) { used[i] = true; used[lockOf[i]] = true; }
            var random = new SeededRandom(seed);
            for (int pair = 0; pair < pairs; pair++)
            {
                int[] depth = Depths(deps, n); if (depth == null) break;
                var blocks = new int[n]; for (int i = 0; i < n; i++) foreach (int d in deps[i]) blocks[d]++;
                int free = 0; for (int i = 0; i < n; i++) if (deps[i].Count == 0) free++;
                int maxDepth = 0; foreach (int d in depth) maxDepth = Math.Max(maxDepth, d);
                // Candidate padlocks: shallow arrows that hold others up. Candidate keys: the deepest arrows.
                var locks = new List<int>(); var keys = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    if (used[i]) continue;
                    if (depth[i] <= 1 && blocks[i] >= 1 && arrows[i].Length >= 4 && !(deps[i].Count == 0 && free <= minFree)) locks.Add(i);
                    if (depth[i] >= Math.Max(2, maxDepth * 6 / 10)) keys.Add(i);
                }
                if (locks.Count == 0 || keys.Count == 0) break;
                locks.Sort((a, b) => blocks[b] != blocks[a] ? blocks[b].CompareTo(blocks[a]) : a.CompareTo(b));
                if (locks.Count > 24) locks.RemoveRange(24, locks.Count - 24);
                bool placed = false;
                // Shuffle the keys, then take the first pair whose key does not wait on its own padlock.
                for (int i = keys.Count - 1; i > 0; i--) { int j = random.NextInt(i + 1); int t = keys[i]; keys[i] = keys[j]; keys[j] = t; }
                foreach (int l in locks)
                {
                    foreach (int k in keys)
                    {
                        if (l == k || used[l] || used[k] || Reaches(deps, k, l)) continue;
                        deps[l].Add(k); lockOf[l] = k; used[l] = used[k] = true; placed = true; break;
                    }
                    if (placed) break;
                }
                if (!placed) break;
            }
            var result = new Board(board.Width, board.Height);
            for (int i = 0; i < n; i++) result.TryAddArrow(lockOf[i] >= 0 ? arrows[i].WithLock(arrows[lockOf[i]].Id) : arrows[i].WithLock(0), out _);
            return result;
        }

        /// <summary>Number of padlocked arrows on the board.</summary>
        public static int CountLocks(Board board) { int c = 0; if (board != null) foreach (Arrow a in board.Arrows) if (a.LockedBy != 0) c++; return c; }

        // Longest dependency chain under each arrow; null when the graph has a cycle.
        private static int[] Depths(List<int>[] deps, int n)
        {
            var depth = new int[n]; var state = new byte[n];
            for (int s = 0; s < n; s++)
            {
                if (state[s] == 2) continue;
                var stack = new Stack<(int node, int next)>(); stack.Push((s, 0)); state[s] = 1;
                while (stack.Count > 0)
                {
                    var (node, next) = stack.Pop();
                    if (next < deps[node].Count)
                    {
                        stack.Push((node, next + 1)); int c = deps[node][next];
                        if (state[c] == 1) return null;
                        if (state[c] == 0) { state[c] = 1; stack.Push((c, 0)); }
                    }
                    else { int d = 0; foreach (int c in deps[node]) d = Math.Max(d, depth[c] + 1); depth[node] = d; state[node] = 2; }
                }
            }
            return depth;
        }

        private static bool Reaches(List<int>[] deps, int from, int target)
        {
            var seen = new bool[deps.Length]; var stack = new Stack<int>(); stack.Push(from); seen[from] = true;
            while (stack.Count > 0) { int x = stack.Pop(); if (x == target) return true; foreach (int c in deps[x]) if (!seen[c]) { seen[c] = true; stack.Push(c); } }
            return false;
        }
    }
}
