using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Reuses one blocker connector and one independent tick-driven hint layer.</summary>
    public sealed class GuideController : MonoBehaviour
    {
        private BoardView boardView;
        private SpriteRenderer line, marker;
        private Material lineMaterial, markerMaterial;
        private ArrowView hinted;
        private float lineElapsed, hintElapsed;
        /// <summary>Whether automatic frames are frozen.</summary>
        public bool IsPaused { get; private set; }
        /// <summary>Whether a connector is currently visible.</summary>
        public bool HasActiveLine { get; private set; }
        /// <summary>Whether a live view still carries the hint.</summary>
        public bool HasActiveHint => hinted != null && hinted.HasHint;
        /// <summary>Current hinted id, or -1.</summary>
        public int HintArrowId => HasActiveHint ? hinted.Arrow.Id : -1;
        /// <summary>Inset line start in world coordinates.</summary>
        public Vector2 LineStartWorld { get; private set; }
        /// <summary>Inset line end in world coordinates.</summary>
        public Vector2 LineEndWorld { get; private set; }

        /// <summary>Binds to board replacements and clears any previous feedback.</summary>
        public void Bind(BoardView value)
        {
            ClearAll();
            if (boardView != null) boardView.BoardChanged -= ClearAll;
            boardView = value;
            if (boardView != null) boardView.BoardChanged += ClearAll;
        }
        /// <summary>Freezes automatic animation without clearing either guide.</summary>
        public void SetPaused(bool paused) => IsPaused = paused;
        /// <summary>Replaces the connector with valid forward geometry; invalid requests show nothing.</summary>
        public void ShowBlockerLine(Arrow tapped, Arrow blocker)
        {
            if (IsPaused) return;
            ClearLine();
            if (boardView == null || !GuideGeometry.TryComputeSegment(tapped, blocker, ViewTheme.GuideInset, out GuideSegment segment)) return;
            EnsureLineObjects();
            LineStartWorld = boardView.CellToWorld(segment.StartX, segment.StartY);
            LineEndWorld = boardView.CellToWorld(segment.EndX, segment.EndY);
            line.transform.position = (LineStartWorld + LineEndWorld) * 0.5f;
            line.transform.rotation = Quaternion.Euler(0, 0, segment.IsHorizontal ? 0 : 90);
            line.transform.localScale = new Vector3(segment.Length, ViewTheme.GuideLineThickness, 1);
            marker.transform.position = LineEndWorld;
            marker.transform.localScale = Vector3.one * ViewTheme.GuideMarkerSize;
            SetLineAlpha(1);
            line.gameObject.SetActive(true); marker.gameObject.SetActive(true);
            HasActiveLine = true;
        }
        /// <summary>Replaces a hint pulse without modifying board state or the connector.</summary>
        public void ShowHint(Arrow arrow)
        {
            if (IsPaused) return;
            ClearHint();
            if (boardView == null || arrow == null || !boardView.TryGetArrowView(arrow.Id, out ArrowView view) || view == null) return;
            hinted = view;
            hinted.SetHint(ViewTheme.HintTint, 1);
        }
        /// <summary>Hides only the reusable line and marker.</summary>
        public void ClearLine()
        {
            HasActiveLine = false; lineElapsed = 0;
            LineStartWorld = LineEndWorld = Vector2.zero;
            if (line != null) line.gameObject.SetActive(false);
            if (marker != null) marker.gameObject.SetActive(false);
        }
        /// <summary>Clears only the hint layer, preserving highlight and flash feedback.</summary>
        public void ClearHint()
        {
            if (hinted != null) hinted.ClearHint();
            hinted = null; hintElapsed = 0;
        }
        /// <summary>Clears both independent guides.</summary>
        public void ClearAll() { ClearHint(); ClearLine(); }
        /// <summary>Advances explicit time, even when paused; invalid time is ignored.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (HasActiveLine)
            {
                lineElapsed = Mathf.Min(ViewTheme.GuideLineDuration, lineElapsed + deltaTime);
                if (lineElapsed >= ViewTheme.GuideLineDuration) ClearLine();
                else SetLineAlpha(AnimationMath.SlideAlpha(lineElapsed / ViewTheme.GuideLineDuration, ViewTheme.GuideFadeStart));
            }
            if (!HasActiveHint) { ClearHint(); return; }
            float duration = ViewTheme.HintPulseCount * ViewTheme.HintPulsePeriod;
            hintElapsed = Mathf.Min(duration, hintElapsed + deltaTime);
            if (hintElapsed >= duration - 0.000001f) { ClearHint(); return; }
            float t = (hintElapsed % ViewTheme.HintPulsePeriod) / ViewTheme.HintPulsePeriod;
            hinted.SetHint(ViewTheme.HintTint, AnimationMath.PulseScale(t, ViewTheme.HintPulsePeak));
        }
        /// <summary>Automatic-frame entry point; tests verify pause by calling this versus Tick.</summary>
        public void AdvanceFrame(float deltaTime) { if (!IsPaused) Tick(deltaTime); }
        private void Update() => AdvanceFrame(Time.deltaTime);
        private void SetLineAlpha(float alpha)
        {
            Color color = ViewTheme.GuideLineColor; color.a *= alpha;
            if (line != null) line.color = color;
            if (marker != null) marker.color = color;
        }
        private void EnsureLineObjects()
        {
            if (line == null) line = CreateRenderer("Blocker Guide Line", SpriteFactory.GetSquareSprite(), ref lineMaterial);
            if (marker == null) marker = CreateRenderer("Blocker Guide Marker", SpriteFactory.GetCellSprite(), ref markerMaterial);
        }
        private SpriteRenderer CreateRenderer(string name, Sprite sprite, ref Material material)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            if (material == null) material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            // Separate explicit textures avoid URP reusing the marker texture for the line.
            material.mainTexture = sprite.texture;
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = ViewTheme.GuideSortingOrder;
            return renderer;
        }
        private void OnDisable() => ClearAll();
        private void OnDestroy()
        {
            Bind(null);
            if (lineMaterial != null) DestroyMaterial(lineMaterial);
            if (markerMaterial != null) DestroyMaterial(markerMaterial);
        }
        private static void DestroyMaterial(Material material)
        {
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
