using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Step 15: settings for <see cref="DenseLevelGenerator"/>.</summary>
    public sealed class DenseLevelSettings
    {
        /// <summary>Shortest arrow (cells).</summary>
        public int MinLength { get; }
        /// <summary>Longest arrow (cells).</summary>
        public int MaxLength { get; }
        /// <summary>Per-thousand chance that a growing arrow turns rather than goes straight.</summary>
        public int TurnPermille { get; }
        /// <summary>Per-thousand of arrows that coil into spirals (always turning the same way).</summary>
        public int SpiralPermille { get; }
        /// <summary>Per-thousand of arrows drawn from the long range (above twice the minimum).</summary>
        public int LongPermille { get; }
        /// <summary>Stop when at least this per-thousand of the silhouette is covered.</summary>
        public int CoveragePermille { get; }
        /// <summary>Creates validated settings.</summary>
        public DenseLevelSettings(int minLength = 3, int maxLength = 24, int turnPermille = 450, int spiralPermille = 120, int longPermille = 250, int coveragePermille = 930)
        {
            if (minLength < 2 || maxLength < minLength || maxLength > 64) throw new ArgumentException("Arrow lengths must satisfy 2 <= min <= max <= 64.");
            MinLength = minLength; MaxLength = maxLength; TurnPermille = Clamp(turnPermille); SpiralPermille = Clamp(spiralPermille); LongPermille = Clamp(longPermille); CoveragePermille = Clamp(coveragePermille);
        }
        private static int Clamp(int v) => Math.Max(0, Math.Min(1000, v));
    }

    /// <summary>
    /// Step 15: fills a silhouette almost completely with winding arrows, the way the big late levels of the genre look.
    /// Paths are grown by a Warnsdorff random walk (so they rarely trap themselves), each path picks one end as its head,
    /// and every cycle in the "must leave first" graph is broken by flipping a head to its other end or, as a last
    /// resort, dropping the arrow. The result is always solvable, densely packed, and only arrows on the rim start free.
    /// </summary>
    public static class DenseLevelGenerator
    {
        /// <summary>Diagnostics of the last run: covered cells after growth, after merging, and arrows dropped while peeling.</summary>
        public static int LastGrown, LastMerged, LastDropped;
        /// <summary>Builds a solvable board inside <paramref name="mask"/>; deterministic for a seed.</summary>
        public static Board Generate(ShapeMask mask, DenseLevelSettings settings, int seed)
        {
            if (mask == null) throw new ArgumentNullException(nameof(mask)); if (settings == null) throw new ArgumentNullException(nameof(settings));
            var random = new SeededRandom(seed);
            int w = mask.Width, h = mask.Height;
            var owner = new int[w, h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) owner[x, y] = -1;
            var paths = new List<List<Cell>>();
            // 1. Cover the silhouette with random-walk paths, scanning start cells in a shuffled order.
            var order = new List<Cell>(); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (mask.Contains(x, y)) order.Add(new Cell(x, y));
            for (int i = order.Count - 1; i > 0; i--) { int j = random.NextInt(i + 1); var t = order[i]; order[i] = order[j]; order[j] = t; }
            foreach (Cell start in order)
            {
                if (owner[start.X, start.Y] >= 0) continue;
                int target = DrawLength(settings, random); bool spiral = random.NextInt(1000) < settings.SpiralPermille; int spin = random.NextInt(2) == 0 ? 1 : -1;
                var cells = new List<Cell> { start }; owner[start.X, start.Y] = paths.Count;
                while (cells.Count < target)
                {
                    Cell tail = cells[cells.Count - 1]; Cell? previous = cells.Count > 1 ? cells[cells.Count - 2] : (Cell?)null;
                    var choices = new List<Cell>(); int fewest = 9;
                    for (int d = 0; d < 4; d++)
                    {
                        var n = new Cell(tail.X + ((Direction)d).GetDx(), tail.Y + ((Direction)d).GetDy());
                        if (!mask.Contains(n.X, n.Y) || owner[n.X, n.Y] >= 0) continue;
                        int free = 0; for (int k = 0; k < 4; k++) { int ax = n.X + ((Direction)k).GetDx(), ay = n.Y + ((Direction)k).GetDy(); if (mask.Contains(ax, ay) && owner[ax, ay] < 0) free++; }
                        if (free < fewest) { fewest = free; choices.Clear(); }
                        if (free == fewest || free <= fewest + 1) choices.Add(n);
                    }
                    if (choices.Count == 0) break;
                    Cell next = choices[0];
                    if (previous.HasValue && choices.Count > 1)
                    {
                        int px = tail.X - previous.Value.X, py = tail.Y - previous.Value.Y;
                        Cell straight = new Cell(tail.X + px, tail.Y + py), turned = new Cell(tail.X - py * spin, tail.Y + px * spin);
                        if (spiral && choices.Contains(turned)) next = turned;
                        else
                        {
                            bool turn = random.NextInt(1000) < settings.TurnPermille;
                            var pool = new List<Cell>(); foreach (Cell c in choices) if ((c.X == straight.X && c.Y == straight.Y) != turn) pool.Add(c);
                            if (pool.Count == 0) pool = choices; next = pool[random.NextInt(pool.Count)];
                        }
                    }
                    else next = choices[random.NextInt(choices.Count)];
                    cells.Add(next); owner[next.X, next.Y] = paths.Count;
                }
                paths.Add(cells);
            }
            // 2. Merge fragments shorter than the minimum into a neighbouring path end, or leave them empty.
            for (int p = 0; p < paths.Count; p++)
            {
                var cells = paths[p]; if (cells == null || cells.Count >= settings.MinLength) continue;
                bool merged = false;
                foreach (bool atEnd in new[] { true, false })
                {
                    Cell end = atEnd ? cells[cells.Count - 1] : cells[0];
                    for (int d = 0; d < 4 && !merged; d++)
                    {
                        int nx = end.X + ((Direction)d).GetDx(), ny = end.Y + ((Direction)d).GetDy(); if (!mask.Contains(nx, ny)) continue;
                        int q = owner[nx, ny]; if (q < 0 || q == p || paths[q] == null || paths[q].Count + cells.Count > settings.MaxLength) continue;
                        var other = paths[q]; var n = new Cell(nx, ny); var reversed = new List<Cell>(cells); reversed.Reverse();
                        if (Same(other[other.Count - 1], n)) other.AddRange(atEnd ? reversed : cells);
                        else if (Same(other[0], n)) { var joined = new List<Cell>(atEnd ? cells : reversed); joined.AddRange(other); paths[q] = joined; }
                        else continue;
                        foreach (Cell c in cells) owner[c.X, c.Y] = q; merged = true;
                    }
                    if (merged) break;
                }
                if (!merged) foreach (Cell c in cells) owner[c.X, c.Y] = -1;
                paths[p] = null;
            }
            LastGrown = 0; foreach (var c in paths) if (c != null) LastGrown += c.Count; LastMerged = 0; LastDropped = 0;
            // 3. Choose heads, then break every cycle in the dependency graph.
            var arrows = new List<List<Cell>>(); foreach (var c in paths) if (c != null && c.Count >= settings.MinLength) arrows.Add(c);
            foreach (var c in arrows) LastMerged += c.Count;
            var heads = new List<bool>(); var alive = new List<bool>();
            var cellOwner = new int[w, h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) cellOwner[x, y] = -1;
            for (int i = 0; i < arrows.Count; i++) { foreach (Cell c in arrows[i]) cellOwner[c.X, c.Y] = i; heads.Add(true); alive.Add(true); }
            // Peel from the outside: repeatedly take an arrow with an end whose ray is already clear of the arrows still
            // waiting, and give it that head. Peeled arrows leave first, so the final board is solvable by construction.
            var remaining = new HashSet<int>(); for (int i = 0; i < arrows.Count; i++) remaining.Add(i);
            var waiting = new int[w, h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) waiting[x, y] = cellOwner[x, y];
            var candidates = new List<(int arrow, bool end)>(); var peelOrder = new List<double>(); for (int i = 0; i < arrows.Count; i++) peelOrder.Add(-1); int step = 0;
            while (remaining.Count > 0)
            {
                candidates.Clear();
                foreach (int i in remaining)
                    foreach (bool end in new[] { true, false })
                        if (ValidHead(arrows[i], end) && ClearRay(arrows[i], end, waiting, i, w, h)) candidates.Add((i, end));
                if (candidates.Count > 0)
                {
                    // Take a few at random per round; each stays clear because removal only empties cells.
                    int take = Math.Max(1, candidates.Count / 3); var taken = new HashSet<int>();
                    for (int k = 0; k < take; k++)
                    {
                        var pick = candidates[random.NextInt(candidates.Count)]; if (!taken.Add(pick.arrow)) continue;
                        heads[pick.arrow] = pick.end; peelOrder[pick.arrow] = step++; remaining.Remove(pick.arrow); foreach (Cell c in arrows[pick.arrow]) waiting[c.X, c.Y] = -1;
                    }
                    continue;
                }
                // Stuck: find the arrow end whose blockers cover the fewest cells, and drop those blockers.
                List<int> cheapest = null; int cheapestCost = int.MaxValue;
                foreach (int i in remaining)
                    foreach (bool end in new[] { true, false })
                    {
                        if (!ValidHead(arrows[i], end)) continue; var blockers = new List<int>(); int cost = 0;
                        Cell head = Head(arrows[i], end), neck = Neck(arrows[i], end); int dx = head.X - neck.X, dy = head.Y - neck.Y;
                        for (int x = head.X + dx, y = head.Y + dy; x >= 0 && y >= 0 && x < w && y < h; x += dx, y += dy) { int o = waiting[x, y]; if (o >= 0 && o != i && !blockers.Contains(o)) { blockers.Add(o); cost += arrows[o].Count; } }
                        if (blockers.Count > 0 && (cost < cheapestCost || cost == cheapestCost && random.NextInt(2) == 0)) { cheapestCost = cost; cheapest = blockers; }
                    }
                if (cheapest == null) { cheapest = new List<int>(); foreach (int i in remaining) { cheapest.Add(i); break; } }
                foreach (int worst in cheapest) { alive[worst] = false; LastDropped++; remaining.Remove(worst); foreach (Cell c in arrows[worst]) { waiting[c.X, c.Y] = -1; cellOwner[c.X, c.Y] = -1; } }
            }
            Refill(mask, settings, random, arrows, heads, alive, peelOrder, cellOwner, w, h);
            if (FindCycle(arrows, heads, alive, cellOwner, w, h) != null) return null;
            var board = new Board(w, h); int id = 1;
            // Number from the top-left like the authored catalog, for stable ids.
            var live = new List<int>(); for (int i = 0; i < arrows.Count; i++) if (alive[i]) live.Add(i);
            live.Sort((a, b) => { Cell ha = Head(arrows[a], heads[a]), hb = Head(arrows[b], heads[b]); return ha.Y != hb.Y ? hb.Y.CompareTo(ha.Y) : ha.X.CompareTo(hb.X); });
            foreach (int i in live)
            {
                var cells = new List<Cell>(arrows[i]); if (!heads[i]) cells.Reverse();
                Cell head = cells[cells.Count - 1], neck = cells[cells.Count - 2];
                board.TryAddArrow(new Arrow(id++, cells, DirectionOf(head.X - neck.X, head.Y - neck.Y)), out _);
            }
            return board;
        }

        // 4. Grow new arrows inside the gaps left by dropped arrows. A new arrow fits at a point of the leaving order
        // after every arrow on its ray and before every arrow whose ray crosses its cells.
        private static void Refill(ShapeMask mask, DenseLevelSettings settings, SeededRandom random, List<List<Cell>> arrows, List<bool> heads, List<bool> alive, List<double> order, int[,] owner, int w, int h)
        {
            var crossing = new List<int>[w, h];
            void AddRay(int i)
            {
                Cell head = Head(arrows[i], heads[i]), neck = Neck(arrows[i], heads[i]); int dx = head.X - neck.X, dy = head.Y - neck.Y;
                for (int x = head.X + dx, y = head.Y + dy; x >= 0 && y >= 0 && x < w && y < h; x += dx, y += dy) (crossing[x, y] ??= new List<int>()).Add(i);
            }
            for (int i = 0; i < arrows.Count; i++) if (alive[i]) AddRay(i);
            for (int pass = 0; pass < 3; pass++)
            {
                var empties = new List<Cell>(); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (mask.Contains(x, y) && owner[x, y] < 0) empties.Add(new Cell(x, y));
                for (int i = empties.Count - 1; i > 0; i--) { int j = random.NextInt(i + 1); var t = empties[i]; empties[i] = empties[j]; empties[j] = t; }
                foreach (Cell start in empties)
                {
                    if (owner[start.X, start.Y] >= 0) continue;
                    int target = pass == 0 ? DrawLength(settings, random) : settings.MinLength + random.NextInt(3);
                    var cells = new List<Cell> { start }; var inPath = new HashSet<Cell> { start };
                    while (cells.Count < target)
                    {
                        Cell tail = cells[cells.Count - 1]; var choices = new List<Cell>();
                        for (int d = 0; d < 4; d++) { var n = new Cell(tail.X + ((Direction)d).GetDx(), tail.Y + ((Direction)d).GetDy()); if (mask.Contains(n.X, n.Y) && owner[n.X, n.Y] < 0 && !inPath.Contains(n)) choices.Add(n); }
                        if (choices.Count == 0) break; var next = choices[random.NextInt(choices.Count)]; cells.Add(next); inPath.Add(next);
                    }
                    // Try trimming from the far end until some head fits.
                    while (cells.Count >= settings.MinLength)
                    {
                        bool done = false;
                        foreach (bool end in random.NextInt(2) == 0 ? new[] { true, false } : new[] { false, true })
                        {
                            if (!ValidHead(cells, end)) continue;
                            Cell head = Head(cells, end), neck = Neck(cells, end); int dx = head.X - neck.X, dy = head.Y - neck.Y;
                            double lo = -1, hi = double.MaxValue;
                            for (int x = head.X + dx, y = head.Y + dy; x >= 0 && y >= 0 && x < w && y < h; x += dx, y += dy) { int o = owner[x, y]; if (o >= 0) lo = Math.Max(lo, order[o]); }
                            foreach (Cell c in cells) if (crossing[c.X, c.Y] != null) foreach (int o in crossing[c.X, c.Y]) if (alive[o]) hi = Math.Min(hi, order[o]);
                            if (!(lo < hi)) continue;
                            int idx = arrows.Count; arrows.Add(new List<Cell>(cells)); heads.Add(end); alive.Add(true);
                            order.Add(hi == double.MaxValue ? lo + 1 : (lo + hi) / 2); foreach (Cell c in cells) owner[c.X, c.Y] = idx; AddRay(idx); done = true; break;
                        }
                        if (done) break;
                        inPath.Remove(cells[cells.Count - 1]); cells.RemoveAt(cells.Count - 1);
                    }
                }
            }
        }

        /// <summary>Covered cells per thousand of the mask.</summary>
        public static int CoveragePermille(Board board, ShapeMask mask) { int c = 0; foreach (Arrow a in board.Arrows) c += a.Length; return mask.Area == 0 ? 0 : c * 1000 / mask.Area; }

        private static int DrawLength(DenseLevelSettings s, SeededRandom r)
        {
            int min = s.MinLength, mid = Math.Min(s.MaxLength, Math.Max(min + 1, 2 * min + 2));
            if (r.NextInt(1000) < s.LongPermille && s.MaxLength > mid) return r.NextInt(mid, s.MaxLength + 1);
            return r.NextInt(min, mid + 1);
        }
        private static bool Same(Cell a, Cell b) => a.X == b.X && a.Y == b.Y;
        private static Cell Head(List<Cell> c, bool atEnd) => atEnd ? c[c.Count - 1] : c[0];
        private static Cell Neck(List<Cell> c, bool atEnd) => atEnd ? c[c.Count - 2] : c[1];
        private static Direction DirectionOf(int dx, int dy) => dx > 0 ? Direction.Right : dx < 0 ? Direction.Left : dy > 0 ? Direction.Up : Direction.Down;
        // A head is valid when no cell of its own body lies on its forward ray.
        private static bool ValidHead(List<Cell> c, bool atEnd)
        {
            Cell head = Head(c, atEnd), neck = Neck(c, atEnd); int dx = head.X - neck.X, dy = head.Y - neck.Y;
            foreach (Cell b in c) { if (Same(b, head)) continue; long ahead = dx != 0 ? (b.Y == head.Y ? (long)(b.X - head.X) * dx : 0) : (b.X == head.X ? (long)(b.Y - head.Y) * dy : 0); if (ahead > 0) return false; }
            return true;
        }
        private static bool ClearRay(List<Cell> c, bool atEnd, int[,] waiting, int self, int w, int h)
        {
            Cell head = Head(c, atEnd), neck = Neck(c, atEnd); int dx = head.X - neck.X, dy = head.Y - neck.Y;
            for (int x = head.X + dx, y = head.Y + dy; x >= 0 && y >= 0 && x < w && y < h; x += dx, y += dy) { int o = waiting[x, y]; if (o >= 0 && o != self) return false; }
            return true;
        }
        private static int RayLength(List<Cell> c, bool atEnd, int w, int h)
        {
            Cell head = Head(c, atEnd), neck = Neck(c, atEnd); int dx = head.X - neck.X, dy = head.Y - neck.Y;
            return dx > 0 ? w - 1 - head.X : dx < 0 ? head.X : dy > 0 ? h - 1 - head.Y : head.Y;
        }
        // Returns the arrows of one dependency cycle (each waits for the next to leave), or null when acyclic.
        private static List<int> FindCycle(List<List<Cell>> arrows, List<bool> heads, List<bool> alive, int[,] owner, int w, int h)
        {
            int n = arrows.Count; var deps = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                deps[i] = new List<int>(); if (!alive[i]) continue;
                Cell head = Head(arrows[i], heads[i]), neck = Neck(arrows[i], heads[i]); int dx = head.X - neck.X, dy = head.Y - neck.Y; int last = -1;
                for (int x = head.X + dx, y = head.Y + dy; x >= 0 && y >= 0 && x < w && y < h; x += dx, y += dy) { int o = owner[x, y]; if (o >= 0 && o != i && o != last && !deps[i].Contains(o)) { deps[i].Add(o); last = o; } }
            }
            var state = new byte[n]; var parent = new int[n];
            for (int s = 0; s < n; s++)
            {
                if (!alive[s] || state[s] != 0) continue;
                var stack = new Stack<(int node, int next)>(); stack.Push((s, 0)); state[s] = 1; parent[s] = -1;
                while (stack.Count > 0)
                {
                    var (node, next) = stack.Pop();
                    if (next < deps[node].Count)
                    {
                        stack.Push((node, next + 1)); int c = deps[node][next];
                        if (state[c] == 1) { var cycle = new List<int> { c }; for (int v = node; v != c && v >= 0; v = parent[v]) cycle.Add(v); return cycle; }
                        if (state[c] == 0) { state[c] = 1; parent[c] = node; stack.Push((c, 0)); }
                    }
                    else state[node] = 2;
                }
            }
            return null;
        }
    }
}
