using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Independent slide-matching timelines; never reads or writes an arrow transform.</summary>
    public sealed class StreakPlayer
    {
        private struct Entry { public bool Active; public int Id, Width, Height; public Arrow Arrow; public float Time; public Vector2 Origin, Direction, Head, Tail; }
        private readonly EffectSpritePool pool;
        private BoardView board;
        private readonly Entry[] entries;
        /// <summary>Creates reusable per-slot state.</summary>
        public StreakPlayer(EffectSpritePool pool, BoardView boardView) { this.pool = pool; board = boardView; entries = new Entry[pool.Capacity]; }
        internal void Bind(BoardView boardView) { ClearAll(); board = boardView; }
        /// <summary>Number of active streak timelines, including initially short hidden streaks.</summary>
        public int ActiveCount { get { int count = 0; for (int i = 0; i < entries.Length; i++) if (entries[i].Active) count++; return count; } }
        /// <summary>Begins a streak at the arrow's original cell center.</summary>
        public void Start(int boardWidth, int boardHeight, Arrow arrow)
        {
            if (arrow == null || board == null) return;
            SpriteRenderer sprite = pool.Acquire(SpriteFactory.GetSquareSprite(),out int slot);
            if (sprite == null) return;
            entries[slot] = new Entry { Active = true, Id = arrow.Id, Width = boardWidth, Height = boardHeight, Arrow = arrow,
                Origin = board.CellToWorld(arrow.X,arrow.Y), Direction = new Vector2(arrow.Direction.GetDx(),arrow.Direction.GetDy()) };
            Apply(slot);
        }
        /// <summary>Advances all streaks without allocating or inspecting arrow views.</summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime >= 0) || float.IsInfinity(deltaTime)) return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].Active) continue;
                entries[i].Time = Mathf.Min(ViewTheme.SlideDuration,entries[i].Time + deltaTime);
                Apply(i);
                if (entries[i].Time >= ViewTheme.SlideDuration || pool.Get(i) == null) { entries[i].Active = false; pool.Release(i); }
            }
        }
        private void Apply(int slot)
        {
            Entry e = entries[slot]; SpriteRenderer sprite = pool.Get(slot); if (sprite == null) return;
            float t = EffectMath.Progress(e.Time,ViewTheme.SlideDuration);
            float distance = EffectMath.StreakHeadDistance(e.Width,e.Height,e.Arrow.X,e.Arrow.Y,e.Arrow.Direction,t);
            e.Head = e.Origin + e.Direction * distance;
            e.Tail = e.Origin + e.Direction * EffectMath.StreakTailDistance(distance,ViewTheme.StreakMaxLength);
            float length = Vector2.Distance(e.Head,e.Tail);
            sprite.transform.position = (e.Head + e.Tail) * .5f;
            sprite.transform.localScale = e.Direction.x != 0 ? new Vector3(length,ViewTheme.StreakThickness,1) : new Vector3(ViewTheme.StreakThickness,length,1);
            Color color = ViewTheme.StreakColor; color.a = EffectMath.StreakAlpha(t,ViewTheme.StreakPeakAlpha,ViewTheme.StreakFadeStart); sprite.color = color;
            sprite.gameObject.SetActive(length >= .05f); entries[slot] = e;
        }
        /// <summary>Reads an active streak's head for synchronization tests.</summary>
        public bool TryGetHeadWorld(int id, out Vector2 head) { for (int i = 0; i < entries.Length; i++) if (entries[i].Active && entries[i].Id == id) { head = entries[i].Head; return true; } head = default; return false; }
        /// <summary>Reads an active streak's tail.</summary>
        public bool TryGetTailWorld(int id, out Vector2 tail) { for (int i = 0; i < entries.Length; i++) if (entries[i].Active && entries[i].Id == id) { tail = entries[i].Tail; return true; } tail = default; return false; }
        /// <summary>Ends all timelines and hides their pooled objects.</summary>
        public void ClearAll() { for (int i = 0; i < entries.Length; i++) entries[i].Active = false; pool.ReleaseAll(); }
    }
}
