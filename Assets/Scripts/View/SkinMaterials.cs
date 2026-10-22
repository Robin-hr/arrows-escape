using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>Shared runtime materials from explicit asset references, never Shader.Find.</summary>
    public static class SkinMaterials
    {
        private static readonly Material[] ribbons=new Material[20];
        private static readonly Material[] glows=new Material[2];
        private static readonly Material[] artMaterials=new Material[2];
        /// <summary>True selects saturated alpha halos on a light board; dark uses additive.</summary>
        public static bool IsLight(Color background)=>background.grayscale>=.45f;
        /// <summary>One shared material per recipe and light/dark mode.</summary>
        public static Material Ribbon(ArrowSkin skin,bool light)
        {
            int i=skin.Index*2+(light?1:0);if(ribbons[i]!=null)return ribbons[i];var resources=ArrowSkinResources.Load();if(resources==null||resources.Ribbon==null)return null;
            var m=new Material(resources.Ribbon){name="Skin ribbon "+skin.Id,hideFlags=HideFlags.HideAndDontSave};m.SetFloat("_SkinIndex",skin.Index);m.SetColor("_Primary",skin.Primary);m.SetColor("_Secondary",skin.Secondary);m.SetFloat("_Period",skin.Period);m.SetFloat("_LightBoard",light?1:0);var art=resources.Find(skin.Id);if(art!=null&&art.Shaft!=null)m.SetTexture("_MainTex",skin.Index==8&&resources.Find("jade_serpent")?.Shaft!=null?resources.Find("jade_serpent").Shaft:art.Shaft);ribbons[i]=m;return m;
        }
        /// <summary>Shared halo/art/particle pass. Alpha on paper, additive on dark.</summary>
        public static Material Glow(bool light)
        {
            int i=light?1:0;if(glows[i]!=null)return glows[i];var resources=ArrowSkinResources.Load();if(resources==null||resources.Glow==null)return null;
            var m=new Material(resources.Glow){name=light?"Skin alpha halo":"Skin additive halo",hideFlags=HideFlags.HideAndDontSave};m.SetFloat("_LightBoard",light?1:0);m.SetInt("_DstBlend",(int)(light?BlendMode.OneMinusSrcAlpha:BlendMode.One));glows[i]=m;return m;
        }
        /// <summary>Reference art stays normally alpha blended, including black obsidian on dark boards.</summary>
        public static Material Art(bool light)
        {
            int i=light?1:0;if(artMaterials[i]!=null)return artMaterials[i];var r=ArrowSkinResources.Load();if(r==null||r.Glow==null)return null;
            var m=new Material(r.Glow){name="Skin reference art",hideFlags=HideFlags.HideAndDontSave};m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetFloat("_Art",1);m.SetFloat("_LightBoard",light?1:0);artMaterials[i]=m;return m;
        }
        /// <summary>Explicit cleanup for editor reloads and tests.</summary>
        public static void Clear()
        {for(int i=0;i<ribbons.Length;i++){Dispose(ribbons[i]);ribbons[i]=null;}for(int i=0;i<glows.Length;i++){Dispose(glows[i]);glows[i]=null;Dispose(artMaterials[i]);artMaterials[i]=null;}}
        private static void Dispose(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
