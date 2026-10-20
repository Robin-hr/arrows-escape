using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Advances reusable value-type animation entries through explicit ticks, without coroutines.</summary>
    public sealed class ArrowAnimator : MonoBehaviour
    {
        private enum Kind { Slide, Bump, Flash }
        private struct Entry
        {
            public ArrowView View;
            public Kind Kind;
            public Vector2 Direction;
            public float Distance, Time, Duration, Peak;
            public Color Tint;
        }

        private readonly List<Entry> entries = new List<Entry>(32);

        /// <summary>Whether any animation entries remain.</summary>
        public bool HasActiveAnimations => entries.Count > 0;

        /// <summary>Slides a released view outward and destroys it when the visual finishes.</summary>
        public void PlaySlideOut(ArrowView view, Direction direction, int boardWidth, int boardHeight, int x, int y)
        {
            if (view == null) return;
            StopForView(view);
            view.ResetVisual();
            view.SetSortingOrder(ViewTheme.SlidingSortingOrder);
            entries.Add(new Entry
            {
                View = view, Kind = Kind.Slide, Duration = ViewTheme.SlideDuration,
                Direction = new Vector2(direction.GetDx(), direction.GetDy()),
                Distance = AnimationMath.SlideDistance(boardWidth, boardHeight, x, y, direction)
            });
        }

        /// <summary>Restarts a clean forward bump and independently flashes the nearest blocker.</summary>
        public void PlayBlocked(ArrowView tapped, Direction tappedDirection, ArrowView blocker)
        {
            if (tapped != null && !IsSliding(tapped))
            {
                StopLayer(tapped, Kind.Bump);
                tapped.SetOffset(Vector2.zero);
                entries.Add(new Entry
                {
                    View = tapped, Kind = Kind.Bump, Duration = ViewTheme.BumpDuration,
                    Direction = new Vector2(tappedDirection.GetDx(), tappedDirection.GetDy())
                });
                PlayFlash(tapped, ViewTheme.BlockedTint, ViewTheme.BumpDuration, 1);
            }
            if (blocker != null && !IsSliding(blocker))
                PlayFlash(blocker, ViewTheme.BlockerTint, ViewTheme.FlashDuration, ViewTheme.BlockerPulsePeak);
        }

        /// <summary>Advances entries without allocations; invalid or negative time is ignored.</summary>
        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0) return;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];
                if (entry.View == null) { entries.RemoveAt(i); continue; }
                entry.Time = Mathf.Min(entry.Duration, entry.Time + deltaTime);
                float t = entry.Time / entry.Duration;
                Advance(entry, t);
                if (t >= 1)
                {
                    Finish(entry);
                    entries.RemoveAt(i);
                }
                else entries[i] = entry;
            }
        }

        /// <summary>Cancels feedback and destroys released slides so exited arrows never reappear.</summary>
        public void StopAll()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];
                if (entry.View == null) continue;
                if (entry.Kind == Kind.Slide) DestroyView(entry.View);
                else entry.View.ResetVisual();
            }
            entries.Clear();
        }

        /// <summary>Stops every entry for this view without destroying it or changing other views.</summary>
        public void Cancel(ArrowView view)
        {
            if (ReferenceEquals(view, null)) return;
            StopForView(view);
            if (view != null) view.ResetVisual();
        }

        private void PlayFlash(ArrowView view, Color tint, float duration, float peak)
        {
            StopLayer(view, Kind.Flash);
            view.SetFlash(tint, 1);
            entries.Add(new Entry { View = view, Kind = Kind.Flash, Tint = tint, Duration = duration, Peak = peak });
        }

        private static void Advance(Entry entry, float t)
        {
            switch (entry.Kind)
            {
                case Kind.Slide:
                    entry.View.SetOffset(entry.Direction * (entry.Distance * AnimationMath.EaseInQuad(t)));
                    entry.View.SetAlpha(AnimationMath.SlideAlpha(t));
                    break;
                case Kind.Bump:
                    entry.View.SetOffset(entry.Direction * AnimationMath.BumpOffset(t, ViewTheme.BumpAmplitude));
                    break;
                case Kind.Flash:
                    entry.View.SetFlash(entry.Tint, AnimationMath.PulseScale(t, entry.Peak));
                    break;
            }
        }

        private static void Finish(Entry entry)
        {
            if (entry.Kind == Kind.Slide) DestroyView(entry.View);
            else RestoreLayer(entry);
        }

        private static void RestoreLayer(Entry entry)
        {
            if (entry.View == null) return;
            if (entry.Kind == Kind.Bump) entry.View.SetOffset(Vector2.zero);
            else if (entry.Kind == Kind.Flash) entry.View.ClearFlash();
        }

        private void StopLayer(ArrowView view, Kind kind)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].View == view && entries[i].Kind == kind)
                {
                    RestoreLayer(entries[i]);
                    entries.RemoveAt(i);
                }
        }

        private void StopForView(ArrowView view)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].View == view) entries.RemoveAt(i);
        }

        private bool IsSliding(ArrowView view)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].View == view && entries[i].Kind == Kind.Slide) return true;
            return false;
        }

        private static void DestroyView(ArrowView view)
        {
            if (view == null) return;
            if (Application.isPlaying) Destroy(view.gameObject);
            else DestroyImmediate(view.gameObject);
        }

        /// <summary>Pause affects automatic frames, never explicit test ticks.</summary>
        public bool IsPaused { get; private set; }
        /// <summary>Sets the automatic-animation pause flag.</summary>
        public void SetPaused(bool paused) => IsPaused = paused;
        /// <summary>Advances a play frame only when unpaused; tests can exercise this path directly.</summary>
        public void AdvanceFrame(float deltaTime) { if (!IsPaused) Tick(deltaTime); }
        private void Update() => AdvanceFrame(Time.deltaTime);
        private void OnDisable() => StopAll();
    }
}
