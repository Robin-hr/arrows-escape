using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Subtle display-case glow; no hierarchy or color allocations during a frame.</summary>
    public sealed class SkinCardPulse : MonoBehaviour
    {
        private static Sprite displayCase;
        /// <summary>Cached procedural vignette; no image asset or material is imported.</summary>
        public static Sprite CaseSprite
        {
            get
            {
                if(displayCase!=null)return displayCase;
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Skin display-case vignette",hideFlags=HideFlags.HideAndDontSave};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float edge=Mathf.Clamp01(new Vector2((x-15.5f)/22,(y-15.5f)/22).sqrMagnitude);texture.SetPixel(x,y,Color.Lerp(new Color32(18,22,28,255),new Color32(7,10,15,255),edge));}
                texture.Apply();displayCase=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,32);displayCase.hideFlags=HideFlags.HideAndDontSave;return displayCase;
            }
        }
        private UnityEngine.UI.RawImage image;
        private float clock;
        /// <summary>Captures the existing reference preview.</summary>
        public void Bind(UnityEngine.UI.RawImage target){image=target;}
        /// <summary>Explicit gentle alpha pulse, leaving the actual source texture unchanged.</summary>
        public void Tick(float dt){if(image==null)return;clock+=dt;float a=.9f+.1f*Mathf.Sin(clock*2);image.color=new Color(1,1,1,a);}
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
    }
}
