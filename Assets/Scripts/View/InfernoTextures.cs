using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Deterministic runtime-only fire textures. No source art or pixel reference is consulted.</summary>
    public static class InfernoTextures
    {
        private static Texture2D noise,glow,dot,streak,headDistance;
        /// <summary>Seamless 256-square R8 fractal value noise.</summary>
        public static Texture2D Noise=>noise!=null?noise:noise=CreateNoise();
        /// <summary>Soft 64-square radial halo.</summary>
        public static Texture2D Glow=>glow!=null?glow:glow=CreateSpot(false,true);
        /// <summary>Soft round 64-square spark.</summary>
        public static Texture2D Dot=>dot!=null?dot:dot=CreateSpot(false,false);
        /// <summary>Elongated 64-square spark streak.</summary>
        public static Texture2D Streak=>streak!=null?streak:streak=CreateSpot(true,false);
        /// <summary>Signed distance from authored curves, not reference pixels; floating point preserves sharp contours.</summary>
        public static Texture2D HeadDistance=>headDistance!=null?headDistance:headDistance=CreateHeadDistance();
        private static Texture2D CreateHeadDistance()
        {
            var data=new float[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++)data[y*256+x]=ArrowEscape.Core.InfernoDesign.HeadDistance(x/255f*2.1f-.6f,y/255f*1.6f-.8f);
            var texture=new Texture2D(256,256,TextureFormat.RFloat,false,true){name="Generated authored Inferno signed distance",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};texture.SetPixelData(data,0);texture.Apply();return texture;
        }
        private static float Hash(int x,int y)
        {unchecked{uint n=(uint)x*374761393u+(uint)y*668265263u+7177u;n=(n^(n>>13))*1274126177u;return (n^(n>>16))/4294967295f;}}
        private static float Value(float x,float y,int period)
        {
            int ix=(int)x,iy=(int)y;float fx=x-ix,fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix%period,iy%period),Hash((ix+1)%period,iy%period),fx),Mathf.Lerp(Hash(ix%period,(iy+1)%period),Hash((ix+1)%period,(iy+1)%period),fx),fy);
        }
        private static Texture2D CreateNoise()
        {
            var data=new byte[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {float sum=0,weight=0,a=1;for(int k=0;k<5;k++){int p=4<<k;sum+=Value(x/255f*p,y/255f*p,p)*a;weight+=a;a*=.5f;}data[y*256+x]=(byte)Mathf.RoundToInt(sum/weight*255);}
            var texture=new Texture2D(256,256,TextureFormat.R8,false,true){name="Generated Inferno tileable fire noise",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};texture.SetPixelData(data,0);texture.Apply();return texture;
        }
        private static Texture2D CreateSpot(bool elongated,bool halo)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="Generated Inferno "+(elongated?"streak":halo?"halo":"spark"),hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var pixels=new Color32[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float u=(x/63f-.5f)*2,v=(y/63f-.5f)*2;float d=u*u+v*v*(elongated?12:1);float alpha=halo?Mathf.Exp(-d*4)*Mathf.SmoothStep(1,0,Mathf.Sqrt(d)):Mathf.Pow(Mathf.Clamp01(1-d),2);pixels[y*64+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(alpha*255));}texture.SetPixels32(pixels);texture.Apply();return texture;
        }
        /// <summary>Releases the generated cache; destroyed textures recreate on demand.</summary>
        public static void Clear(){Destroy(noise);Destroy(glow);Destroy(dot);Destroy(streak);Destroy(headDistance);noise=glow=dot=streak=headDistance=null;}
        private static void Destroy(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
