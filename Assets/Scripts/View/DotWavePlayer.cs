using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Temporarily layers a radial pulse over captured dot colors and scales.</summary>
    public sealed class DotWavePlayer
    {
        private SpriteRenderer[] dots = System.Array.Empty<SpriteRenderer>();
        private Color[] colors = System.Array.Empty<Color>();
        private Vector3[] scales = System.Array.Empty<Vector3>();
        private float[] delays = System.Array.Empty<float>();
        private float elapsed, total;
        /// <summary>Whether a wave still owns captured dot values.</summary>
        public bool IsActive { get; private set; }
        /// <summary>Captures dots once; reuses buffers when the cell count is unchanged.</summary>
        public bool Start(BoardView boardView, int width, int height, int originX, int originY)
        {
            if (boardView == null || !boardView.HasCellDots) return false;
            ClearAll(); int count = width * height;
            if (dots.Length != count) { dots = new SpriteRenderer[count]; colors = new Color[count]; scales = new Vector3[count]; delays = new float[count]; }
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                int i = y * width + x; boardView.TryGetCellDot(x,y,out dots[i]);
                if (dots[i] != null) { colors[i] = dots[i].color; scales[i] = dots[i].transform.localScale; }
                delays[i] = EffectMath.DotWaveDelay(x,y,originX,originY,ViewTheme.DotWaveSecondsPerCell);
            }
            elapsed = 0; total = EffectMath.DotWaveTotalSeconds(width,height,originX,originY,ViewTheme.DotWaveSecondsPerCell,ViewTheme.DotWaveDuration);
            IsActive = true; return true;
        }
        /// <summary>Advances the captured dots, skipping destroyed Unity objects.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive || !(deltaTime >= 0) || float.IsInfinity(deltaTime)) return;
            elapsed += deltaTime; if (elapsed >= total) { ClearAll(); return; }
            for (int i = 0; i < dots.Length; i++) {
                if (dots[i] == null) continue;
                float amount = EffectMath.PulseAmount(EffectMath.DotWaveLocalTime(elapsed,delays[i],ViewTheme.DotWaveDuration));
                dots[i].transform.localScale = scales[i] * (1 + (ViewTheme.DotWavePeakScale - 1) * amount);
                dots[i].color = Color.Lerp(colors[i],ViewTheme.DotWaveColor,amount);
            }
        }
        /// <summary>Restores captured values exactly, including when interrupted.</summary>
        public void ClearAll()
        {
            if (!IsActive) return;
            for (int i = 0; i < dots.Length; i++) if (dots[i] != null) { dots[i].color = colors[i]; dots[i].transform.localScale = scales[i]; }
            IsActive = false;
        }
    }
}
