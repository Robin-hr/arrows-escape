using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Reusable number tween; all intermediate strings are cached when a value changes.</summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        private UnityEngine.UI.Text label;
        private string[] frames;
        private float elapsed;
        private int frame=-1,value;
        private bool active;
        /// <summary>Current target coin amount.</summary>
        public int Value=>value;
        /// <summary>Initializes a header counter.</summary>
        public void Bind(UnityEngine.UI.Text target){label=target;}
        /// <summary>Caches thirty steps outside the frame path.</summary>
        public void SetValue(int next,bool animate=true,string prefix="")
        {
            if(label==null)return;
            int start=animate?value:next;value=next;elapsed=0;frame=-1;frames=new string[31];
            for(int i=0;i<=30;i++)frames[i]=prefix+Mathf.RoundToInt(Mathf.Lerp(start,next,i/30f));
            active=animate;label.text=frames[animate?0:30];
        }
        /// <summary>Advances cached text without string allocations.</summary>
        public void Tick(float dt)
        {
            if(!active||!(dt>0)||float.IsInfinity(dt))return;elapsed+=dt;
            int current=Mathf.Min(30,Mathf.FloorToInt(elapsed/.6f*30));if(current!=frame){frame=current;label.text=frames[current];}if(current==30)active=false;
        }
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
    }
}
