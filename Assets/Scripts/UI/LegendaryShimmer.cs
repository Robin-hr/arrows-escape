using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Subtle prepared legendary frame glint; no temporary graphics or allocations.</summary>
    public sealed class LegendaryShimmer : MonoBehaviour
    {
        private UnityEngine.UI.Image frame;
        private Color original;
        private float elapsed;
        /// <summary>Captures the category's exact frame color.</summary>
        public void Bind(UnityEngine.UI.Image target,Color color){frame=target;original=color;}
        /// <summary>Advances a gentle glint on the existing frame.</summary>
        public void Tick(float dt){if(frame==null||!(dt>0)||float.IsInfinity(dt))return;elapsed+=dt;float flash=Mathf.Pow(Mathf.Max(0,Mathf.Sin(elapsed*1.2f)),12)*.3f;frame.color=Color.Lerp(original,Color.white,flash);}
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
    }
}
