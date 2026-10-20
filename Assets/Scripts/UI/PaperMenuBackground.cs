using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Paper dots and four low-opacity drifting ribbon arrows; fixed objects, no frame allocations.</summary>
    public sealed class PaperMenuBackground : MonoBehaviour,IMenuBackground
    {
        private readonly RectTransform[] arrows=new RectTransform[4];
        private RectTransform root;
        private float elapsed;
        /// <summary>Creates one combined dot mesh and four cached ribbon graphics.</summary>
        public void Build(RectTransform parent)
        {
            root=parent;var dots=UiFactory.Rect("FaintDotGrid",parent,Vector2.zero,Vector2.zero);UiFactory.Stretch(dots);dots.gameObject.AddComponent<MenuDotGraphic>().color=new Color(.45f,.55f,.55f,.16f);
            for(int i=0;i<4;i++)
            {
                arrows[i]=UiFactory.Rect("DriftingArrow"+i,parent,new Vector2(440,260),Vector2.zero);
                var graphic=arrows[i].gameObject.AddComponent<PathPreviewGraphic>();graphic.Configure(new Color(.11f,.13f,.16f,.055f),new Color(.11f,.13f,.16f,.035f));
                arrows[i].localRotation=Quaternion.Euler(0,0,i*90+15);
            }
            Tick(.0001f);
        }
        /// <summary>Restarts the deterministic drift.</summary>
        public void Restart(){elapsed=0;}
        /// <summary>Moves existing rectangles; no geometry is regenerated during drift.</summary>
        public void Tick(float dt)
        {
            if(root==null||!(dt>0)||float.IsInfinity(dt))return;elapsed+=dt;
            Vector2 size=root.rect.size;
            for(int i=0;i<4;i++)
            {
                if(arrows[i]==null)continue;
                float progress=Mathf.Repeat(elapsed*.015f+i*.27f,1);
                arrows[i].anchoredPosition=new Vector2((progress-.5f)*(size.x+700),Mathf.Sin(elapsed*.1f+i*1.8f)*size.y*.32f);
            }
        }
    }
}
