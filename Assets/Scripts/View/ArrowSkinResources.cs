using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Explicit player-build shader and art references, loaded without shader-name searches.</summary>
    public sealed class ArrowSkinResources : ScriptableObject
    {
        /// <summary>Mobile unlit ribbon shader.</summary>
        public Shader Ribbon;
        /// <summary>Halo, sprite art and pooled particle shader.</summary>
        public Shader Glow;
        /// <summary>Procedural Inferno head, ribbon, halo and generated spark pass.</summary>
        public Shader Inferno;
        /// <summary>Shared procedural crystal, plasma, smoke and signature pass.</summary>
        public Shader Elemental;
        /// <summary>Shared procedural Rare and theme shader, explicitly retained in player builds.</summary>
        public Shader RareTheme;
        /// <summary>All twenty shop references plus jade_serpent.</summary>
        public ArrowSkinArt[] Art;
        /// <summary>Looks up art without allocating.</summary>
        public ArrowSkinArt Find(string id)
        {if(Art!=null)for(int i=0;i<Art.Length;i++)if(Art[i]!=null&&Art[i].Id==id)return Art[i];return null;}
        /// <summary>Loads the asset whose explicit references protect shaders from stripping.</summary>
        public static ArrowSkinResources Load()=>Resources.Load<ArrowSkinResources>("ArrowSkinResources");
    }
}
