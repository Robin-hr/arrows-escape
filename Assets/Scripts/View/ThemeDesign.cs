using System;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Stable visual identities and dark palettes; independent of inventory persistence.</summary>
    public static class ThemeDesign
    {
        /// <summary>Maximum shader-mote count in a theme's front layer.</summary>
        public const int ParticleLimit=150;
        private static readonly string[] ids={"mystic_wood","frozen_cavern","pharaohs_tomb","volcanic_underworld","neon_cyberpunk","cosmic_nebula"};
        private static readonly Color32[] colors={new Color32(15,36,24,255),new Color32(11,37,64,255),new Color32(59,42,20,255),new Color32(42,14,6,255),new Color32(13,2,33,255),new Color32(11,10,36,255)};
        /// <summary>Returns minus one for Paper or an unknown id.</summary>
        public static int Index(string id){for(int i=0;i<ids.Length;i++)if(ids[i]==id)return i;return -1;}
        /// <summary>Returns the theme base, or exact Paper.</summary>
        public static Color Background(string id){int index=Index(id);return index<0?ViewTheme.Background:colors[index];}
        /// <summary>Light ink on a theme, retaining exact Classic on Paper.</summary>
        public static Color ClassicInk(string id)=>Index(id)<0?ViewTheme.ArrowColor:new Color32(233,238,242,255);
    }
}
