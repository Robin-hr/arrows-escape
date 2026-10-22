using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>One explicit shader shared by the five rare recipes and six layered procedural themes.</summary>
    public static class RareMaterials
    {
        private static readonly Material[] cache=new Material[160];
        /// <summary>Returns a warmed shared material without loading reference art.</summary>
        public static Material Get(int skin,int mode,bool light)
        {
            int index=skin*16+mode*2+(light?1:0);if(cache[index]!=null)return cache[index];
            Shader shader=ArrowSkinResources.Load()?.RareTheme;if(shader==null)return null;
            var material=new Material(shader){name="Procedural rare layer",hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Skin",skin);material.SetFloat("_Mode",mode);material.SetFloat("_LightBoard",light?1:0);
            material.SetInt("_DstBlend",(int)(light||mode<2||skin==5?BlendMode.OneMinusSrcAlpha:BlendMode.One));material.SetTexture("_Noise",InfernoTextures.Noise);cache[index]=material;return material;
        }
        /// <summary>Creates an owned alpha-blended theme pass, configured only at setup.</summary>
        public static Material Theme(int theme,int layer)
        {
            Shader shader=ArrowSkinResources.Load()?.RareTheme;if(shader==null)return null;
            var material=new Material(shader){name="Procedural theme depth",hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Mode",10);material.SetFloat("_Theme",theme);material.SetFloat("_ThemeLayer",layer);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetTexture("_Noise",InfernoTextures.Noise);return material;
        }
    }
}
