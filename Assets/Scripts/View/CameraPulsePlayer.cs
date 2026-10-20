using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>A single gentle background pulse, always restoring the theme background exactly.</summary>
    public sealed class CameraPulsePlayer
    {
        private Camera camera;
        private float elapsed;
        /// <summary>Whether the pulse is running.</summary>
        public bool IsActive { get; private set; }
        /// <summary>Restarts from the same base without accumulating brightness.</summary>
        public void Start()
        {
            ClearAll(); if (camera == null) camera = Camera.main; if (camera == null) return;
            elapsed = 0; camera.backgroundColor = ViewTheme.Background; IsActive = true;
        }
        /// <summary>Blends at most 22 percent toward the slightly lighter color.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive || !(deltaTime >= 0) || float.IsInfinity(deltaTime)) return;
            if (camera == null) { IsActive = false; return; }
            elapsed += deltaTime; float t = EffectMath.Progress(elapsed,ViewTheme.CameraPulseDuration);
            if (t >= 1) { ClearAll(); return; }
            camera.backgroundColor = Color.Lerp(ViewTheme.Background,ViewTheme.CameraPulseColor,ViewTheme.CameraPulseStrength * EffectMath.PulseAmount(t));
        }
        /// <summary>Restores the exact theme background immediately.</summary>
        public void ClearAll() { if (IsActive && camera != null) camera.backgroundColor = ViewTheme.Background; IsActive = false; }
    }
}
