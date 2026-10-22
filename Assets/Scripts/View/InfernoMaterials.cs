using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>Eight shared procedural passes, protected by an explicit shader asset reference.</summary>
    public static class InfernoMaterials
    {
        private static readonly Material[] cache=new Material[10];
        /// <summary>Body, head, halo or particle pass, with paper alpha or dark additive blending.</summary>
        public static Material Get(int mode,bool light)
        {
            int index=mode*2+(light?1:0);if(cache[index]!=null)return cache[index];var resource=ArrowSkinResources.Load();if(resource==null||resource.Inferno==null)return null;
            var material=new Material(resource.Inferno){name="Procedural Inferno "+mode+(light?" alpha":" additive"),hideFlags=HideFlags.HideAndDontSave};material.SetFloat("_Mode",mode);material.SetFloat("_LightBoard",light?1:0);material.SetInt("_DstBlend",(int)(light?BlendMode.OneMinusSrcAlpha:BlendMode.One));material.SetTexture("_Noise",InfernoTextures.Noise);material.SetTexture("_Spark",InfernoTextures.Streak);material.SetTexture("_Dot",InfernoTextures.Dot);material.SetTexture("_Glow",InfernoTextures.Glow);material.SetTexture("_HeadDistance",InfernoTextures.HeadDistance);cache[index]=material;return material;
        }
        /// <summary>Explicit cache disposal for Editor/test reloads.</summary>
        public static void Clear(){for(int i=0;i<cache.Length;i++){if(cache[i]!=null){if(Application.isPlaying)Object.Destroy(cache[i]);else Object.DestroyImmediate(cache[i]);}cache[i]=null;}}
    }
}
