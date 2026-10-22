using System.Collections.Generic;
namespace ArrowEscape.View
{
    /// <summary>Only the first ten released recipes. Every other id keeps the 14B tint fallback.</summary>
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
            new ArrowSkin("crimson_relic",9,"#C8102E","#FF6B7A",1.6f,.65f)};
        /// <summary>Read-only implemented recipes.</summary>
        public static IReadOnlyList<ArrowSkin> All { get; }=System.Array.AsReadOnly(skins);
        /// <summary>Null indicates Classic or a not-yet-built tint fallback.</summary>
        public static ArrowSkin Find(string id){for(int i=0;i<skins.Length;i++)if(skins[i].Id==id)return skins[i];return null;}
    }
}
