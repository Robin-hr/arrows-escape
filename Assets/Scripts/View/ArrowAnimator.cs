using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Advances reusable value-type animation entries through explicit ticks, without coroutines.</summary>
    public sealed class ArrowAnimator : MonoBehaviour
    {
        private enum Kind { Slide, Bump, Flash, IceSlide, PathReturn }
        private struct Entry
        {
            public ArrowView View;
            public Kind Kind;
            public Vector2 Direction;
            public float Distance, Time, Duration, Peak;
            public Color Tint;
            public Vector2 StartOffset;
        }

        private readonly List<Entry> entries = new List<Entry>(32);

        /// <summary>Whether any animation entries remain.</summary>
        public bool HasActiveAnimations => entries.Count > 0;

        /// <summary>Slides a released view outward and destroys it when the visual finishes.</summary>
        public void PlaySlideOut(ArrowView view, Direction direction, int boardWidth, int boardHeight, int x, int y)
        {
            if (view == null) return;
            Vector2 startOffset=view.Offset;
            float pathStart=view.Path!=null?view.Path.Distance:0;
            StopForView(view);
            view.ResetVisual();
            view.SetOffset(startOffset);
            if(view.Path!=null)view.Path.SetDistance(pathStart);
            view.SetSortingOrder(ViewTheme.SlidingSortingOrder);
            entries.Add(new Entry
            {
                View = view, Kind = Kind.Slide, Duration = (view.Path!=null?AnimationMath.PathExitDuration(view.Path.ExitDistance):ViewTheme.SlideDuration)*(view.Skin?.Skin?.Id=="plasma_bolt"?.5f:1),
                StartOffset=startOffset, Peak=pathStart,
                Direction = new Vector2(direction.GetDx(), direction.GetDy()),
                Distance = view.Path!=null?view.Path.ExitDistance:AnimationMath.SlideDistance(boardWidth, boardHeight, x, y, direction)
            });
        }
        /// <summary>Reverses a path exit into its restored logical cells.</summary>
        public void PlayPathReturn(ArrowView view, float startDistance = -1)
        {
            if(view==null||view.Path==null)return;
            float distance=startDistance<0?view.Path.ExitDistance:startDistance;
            StopForView(view);view.ResetVisual();view.Path.SetDistance(distance);
            entries.Add(new Entry{View=view,Kind=Kind.PathReturn,Distance=distance,Duration=AnimationMath.PathExitDuration(distance)*(view.Skin?.Skin?.Id=="plasma_bolt"?.5f:1)});
        }
        /// <summary>Animates the current offset back to exact zero without changing the new logical base.</summary>
        public void PlayIceSlide(ArrowView view,float durationSeconds)
        {
            if(view==null)return;Vector2 start=view.Offset;StopForView(view);view.ResetVisual();view.SetOffset(start);
            if(!(durationSeconds>0)||float.IsInfinity(durationSeconds)){view.SetOffset(Vector2.zero);return;}
            entries.Add(new Entry{View=view,Kind=Kind.IceSlide,Duration=durationSeconds,StartOffset=start});
        }

        /// <summary>Restarts a clean forward bump and independently flashes the nearest blocker.</summary>
        public void PlayBlocked(ArrowView tapped, Direction tappedDirection, ArrowView blocker)
        {
            if (tapped != null && !IsSliding(tapped))
            {
                StopLayer(tapped, Kind.Bump);
                StopLayer(tapped,Kind.IceSlide);
                tapped.SetOffset(Vector2.zero);
                if(tapped.Path!=null)tapped.Path.SetDistance(0);
                entries.Add(new Entry
                {
                    View = tapped, Kind = Kind.Bump, Duration = tapped.Path!=null?.14f:ViewTheme.BumpDuration,
                    Direction = new Vector2(tappedDirection.GetDx(), tappedDirection.GetDy())
                });
                PlayFlash(tapped, ViewTheme.BlockedTint, tapped.Path!=null?.14f:ViewTheme.BumpDuration, 1);
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

        private static float VisualProgress(ArrowView view,float t,float duration)
        {var skin=view.Skin?.Skin;if(skin==null||!skin.IsProcedural||skin.Index==1)return t;float delay=Mathf.Min(.8f,(skin.Index==2?.12f:(skin.Index>=5&&skin.Index<=9)?.24f:.1f)/duration);return Mathf.Clamp01((t-delay)/(1-delay));}

        private static void Advance(Entry entry, float t)
        {
            switch (entry.Kind)
            {
                case Kind.Slide:
                    float outgoing=VisualProgress(entry.View,t,entry.Duration);
                    if(entry.View.Path!=null)entry.View.Path.SetDistance(Mathf.Lerp(entry.Peak,entry.Distance,AnimationMath.EaseInQuad(outgoing)));
                    else entry.View.SetOffset(entry.StartOffset+entry.Direction * (entry.Distance * AnimationMath.EaseInQuad(outgoing)));
                    if(entry.View.Path==null)entry.View.SetAlpha(AnimationMath.SlideAlpha(t));
                    break;
                case Kind.PathReturn:
                    entry.View.Path.SetDistance(entry.Distance*AnimationMath.EaseInQuad(VisualProgress(entry.View,1-t,entry.Duration)));
                    break;
                case Kind.IceSlide:
                    entry.View.SetOffset(entry.StartOffset*AnimationMath.SlideOffsetFactor(t));
                    break;
                case Kind.Bump:
                    if(entry.View.Path!=null)entry.View.Path.SetDistance(AnimationMath.BumpOffset(t,.15f));
                    else entry.View.SetOffset(entry.Direction * AnimationMath.BumpOffset(t, ViewTheme.BumpAmplitude));
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
            if(entry.Kind==Kind.PathReturn || (entry.Kind==Kind.Bump && entry.View.Path!=null))entry.View.Path.SetDistance(0);
            else if (entry.Kind == Kind.Bump) entry.View.SetOffset(Vector2.zero);
            else if(entry.Kind==Kind.IceSlide)entry.View.SetOffset(Vector2.zero);
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
