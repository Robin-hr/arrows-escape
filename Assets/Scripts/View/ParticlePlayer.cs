using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Bounded preallocated particle timelines, rendered as ordinary circle sprites.</summary>
    public sealed class ParticlePlayer
    {
        private struct Entry { public bool Active; public Vector2 Origin; public ParticleSpec Spec; public Color Color; public float Time; }
        private readonly EffectSpritePool pool;
        private readonly Entry[] entries;
        /// <summary>Allocates state once. Coordinates and sizes are already world units.</summary>
        public ParticlePlayer(EffectSpritePool pool, BoardView boardView) { this.pool = pool; entries = new Entry[pool.Capacity]; }
        /// <summary>Number of live particle timelines.</summary>
        public int ActiveCount { get { int count = 0; for (int i = 0; i < entries.Length; i++) if (entries[i].Active) count++; return count; } }
        /// <summary>Copies supplied specs and colors into reusable slots.</summary>
        public void Spawn(Vector2 originWorld, ParticleSpec[] specs, int count, Color[] colors)
        {
            if (specs == null || colors == null || colors.Length == 0) return;
            count = Mathf.Min(count,specs.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer sprite = pool.Acquire(SpriteFactory.GetCellSprite(),out int slot); if (sprite == null) continue;
                ParticleSpec spec = specs[i]; int index = Mathf.Clamp(spec.ColorIndex,0,colors.Length-1);
                entries[slot] = new Entry { Active = true, Origin = originWorld, Spec = spec, Color = colors[index] }; Apply(slot);
            }
        }
        /// <summary>Moves, shrinks, fades, and releases particles without allocations.</summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime >= 0) || float.IsInfinity(deltaTime)) return;
            for (int i = 0; i < entries.Length; i++) {
                if (!entries[i].Active) continue; entries[i].Time += deltaTime; Apply(i);
                if (EffectMath.Progress(entries[i].Time,entries[i].Spec.Lifetime) >= 1 || pool.Get(i) == null) { entries[i].Active = false; pool.Release(i); }
            }
        }
        private void Apply(int slot)
        {
            SpriteRenderer sprite = pool.Get(slot); if (sprite == null) return; Entry e = entries[slot];
            float t = EffectMath.Progress(e.Time,e.Spec.Lifetime);
            sprite.transform.position = e.Origin + new Vector2(e.Spec.DirX,e.Spec.DirY) * EffectMath.ParticleDistance(e.Spec.Distance,t);
            sprite.transform.localScale = Vector3.one * EffectMath.ParticleSize(e.Spec.StartSize,e.Spec.EndSize,t);
            Color color = e.Color; color.a *= EffectMath.ParticleAlpha(t); sprite.color = color;
        }
        /// <summary>Hides every particle immediately.</summary>
        public void ClearAll() { for (int i = 0; i < entries.Length; i++) entries[i].Active = false; pool.ReleaseAll(); }
    }
}
