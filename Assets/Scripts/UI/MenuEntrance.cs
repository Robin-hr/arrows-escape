using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Prepared staggered entrance and a gentle title underline loop.</summary>
    public sealed class MenuEntrance : MonoBehaviour
    {
        private RectTransform[] items;
        private CanvasGroup[] groups;
        private Vector2[] positions;
        private RectTransform titleArrow;
        private float elapsed;
        /// <summary>Captures exact rest positions once.</summary>
        public void Configure(RectTransform[] elements,RectTransform underline)
        {
            items=elements;groups=new CanvasGroup[items.Length];positions=new Vector2[items.Length];
            for(int i=0;i<items.Length;i++){positions[i]=items[i].anchoredPosition;groups[i]=items[i].gameObject.AddComponent<CanvasGroup>();}
            titleArrow=underline;Restart();
        }
        /// <summary>Restarts fade/slide presentation without recreating elements.</summary>
        public void Restart(){elapsed=0;if(items==null)return;for(int i=0;i<items.Length;i++){groups[i].alpha=0;items[i].anchoredPosition=positions[i]-Vector2.up*12;}}
        /// <summary>Advances cached entrance state and the four-second title loop.</summary>
        public void Tick(float dt)
        {
            if(items==null||!(dt>0)||float.IsInfinity(dt))return;elapsed+=dt;
            for(int i=0;i<items.Length;i++){float t=Mathf.Clamp01((elapsed-i*.06f)/.3f);float ease=1-Mathf.Pow(1-t,3);groups[i].alpha=ease;items[i].anchoredPosition=positions[i]-Vector2.up*(12*(1-ease));}
            titleArrow.anchoredPosition+=Vector2.right*(Mathf.Sin(elapsed*Mathf.PI/2)*12);
        }
    }
}
