using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>Shared parameterized crystal, energy and smoke passes; no reference-image dependency.</summary>
    public static class ElementalMaterials
    {
        private static readonly Material[] cache=new Material[48];
        /// <summary>Returns a cached body, head, halo, atmosphere or signature material.</summary>
        public static Material Get(int skin,int mode,bool light)
        {
            int i=skin*12+mode*2+(light?1:0);if(cache[i]!=null)return cache[i];
            var shader=ArrowSkinResources.Load()?.Elemental;if(shader==null)return null;
            var material=new Material(shader){name="Elemental procedural pass",hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Skin",skin);material.SetFloat("_Mode",mode);material.SetFloat("_LightBoard",light?1:0);
            // Smoke must absorb light rather than add brightness, even on a dark board.
            material.SetInt("_DstBlend",(int)(light||skin==3?BlendMode.OneMinusSrcAlpha:BlendMode.One));
            material.SetTexture("_Noise",InfernoTextures.Noise);material.SetTexture("_Glow",InfernoTextures.Glow);
            cache[i]=material;return material;
        }
    }
}
