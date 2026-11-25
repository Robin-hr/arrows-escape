using System.Collections.Generic;
namespace ArrowEscape.View
{
    /// <summary>Every arrow skin in the shop (Step 16: the ten former tint-only skins now use animated procedural recipes in their own colours).</summary>
    public static class ArrowSkinRegistry
    {
        private static readonly ArrowSkin[] skins={
            new ArrowSkin("frostbite",0,"#8FE3FF","#E6FBFF",2.5f,.6f),
            new ArrowSkin("inferno_streak",1,"#FF5A1F","#FFC93C",1.1f,.45f),
            new ArrowSkin("plasma_bolt",2,"#00F0FF","#FF2BD6",1.5f,.25f),
            new ArrowSkin("void_whisper",3,"#1A0B2E","#A35BFF",3,.8f),
            new ArrowSkin("obsidian_shard",5,"#14151A","#6E7BFF",4,.45f),
            new ArrowSkin("chromium_spike",6,"#C9D1D9","#FF7AE6",5,.55f),
            new ArrowSkin("celestial_quill",7,"#FFD86B","#FFF6D5",2.7f,.9f),
            new ArrowSkin("emerald_viper",8,"#18C37E","#0B6E4F",4,.65f),
            new ArrowSkin("crimson_relic",9,"#C8102E","#FF6B7A",1.6f,.65f),
            new ArrowSkin("cyber_pulsar",2,"#00FF9C","#0AFFD5",1.3f,.3f,"Digital pulses race along its grid."),
            new ArrowSkin("sonic_boom",0,"#E8F4FF","#7FB8FF",2.2f,.5f,"Breaks the sound barrier in a burst of wind."),
            new ArrowSkin("vortex_harpoon",5,"#3A3F4B","#B794F6",3.6f,.5f,"Heavy steel that bends the light around it."),
            new ArrowSkin("stealth_phantom",3,"#9CA3AF","#E5E7EB",3.2f,.7f,"Now you see it. Now you don't."),
            new ArrowSkin("spore_needle",8,"#8B5A2B","#7CFF4F",4.2f,.6f,"Living wood, glowing with ancient moss."),
            new ArrowSkin("bone_carver",6,"#E8DCC2","#C9A27A",5.4f,.5f,"Carved from fossils older than the stars."),
            new ArrowSkin("abyss_fang",9,"#0E7490","#FF7F50",1.9f,.6f,"A glowing fang from the deepest trench."),
            new ArrowSkin("infinity_core",7,"#FFC300","#FFF3B0",2.4f,.9f,"Golden light that bends reality itself."),
            new ArrowSkin("phoenix_wing",1,"#FF7A00","#FFD000",1.0f,.5f,"Rises from its own flames."),
            new ArrowSkin("chronos_needle",6,"#D9E2EC","#7DD3FC",4.6f,.5f,"Every tick of time, sharpened to a point.")};
        /// <summary>Read-only implemented recipes.</summary>
        public static IReadOnlyList<ArrowSkin> All { get; }=System.Array.AsReadOnly(skins);
        /// <summary>Null indicates Classic or a not-yet-built tint fallback.</summary>
        public static ArrowSkin Find(string id){for(int i=0;i<skins.Length;i++)if(skins[i].Id==id)return skins[i];return null;}
    }
}
