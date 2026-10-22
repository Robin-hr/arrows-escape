using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Released cosmetic ids are permanent: never rename or reuse an id after release.</summary>
    public static class ShopCatalog
    {
        private static readonly IReadOnlyList<ShopItem> items = Array.AsReadOnly(new[] {
            new ShopItem("classic","Classic Ink","The original clean ink arrow.",ShopSlot.Arrow,"Classic",0,"#1C2228","#1C2228",true),
            new ShopItem("paper","Paper","A calm paper background with a faint dot grid.",ShopSlot.Theme,"Classic",0,"#E8F2F2","#C5D3D3",true),
            new ShopItem("frostbite","Frostbite Arrow","Leaves a slowing trail of ice crystals.",ShopSlot.Arrow,"Elemental & Magic",150,"#8FE3FF","#E6FBFF",true),
            new ShopItem("inferno_streak","Inferno Streak","A blazing trail of fire that changes color as it speeds up.",ShopSlot.Arrow,"Elemental & Magic",150,"#FF5A1F","#FFC93C",true),
            new ShopItem("plasma_bolt","Plasma Bolt","A futuristic neon laser beam arrow.",ShopSlot.Arrow,"Elemental & Magic",150,"#00F0FF","#FF2BD6",true),
            new ShopItem("void_whisper","Void Whisper","A dark, shadowy arrow wrapped in purple mist.",ShopSlot.Arrow,"Elemental & Magic",150,"#3B1A5A","#A35BFF",true),
            new ShopItem("obsidian_shard","Obsidian Shard","A sleek, glossy black arrow made of volcanic glass.",ShopSlot.Arrow,"Rare & Premium",300,"#14151A","#6E7BFF",true),
            new ShopItem("chromium_spike","Chromium Spike","A reflective, color-changing metallic arrow.",ShopSlot.Arrow,"Rare & Premium",300,"#C9D1D9","#FF7AE6",true),
            new ShopItem("celestial_quill","Celestial Quill","Made from glowing stardust and golden feathers.",ShopSlot.Arrow,"Rare & Premium",300,"#FFD86B","#FFF6D5",true),
            new ShopItem("emerald_viper","Emerald Viper","A vibrant green jade arrow with a serpentine tip.",ShopSlot.Arrow,"Rare & Premium",300,"#18C37E","#0B6E4F",true),
            new ShopItem("crimson_relic","Crimson Relic","A ruby-encrusted arrow that glows with a deep red light.",ShopSlot.Arrow,"Rare & Premium",300,"#C8102E","#FF6B7A",true),
            new ShopItem("cyber_pulsar","Cyber Pulsar","A high-tech arrow with moving digital grid lines.",ShopSlot.Arrow,"Tactical & Sci-Fi",400,"#00FF9C","#0A2A1F",true),
            new ShopItem("sonic_boom","Sonic Boom","A wide-tipped arrow surrounded by a visible wind vortex.",ShopSlot.Arrow,"Tactical & Sci-Fi",400,"#E8F4FF","#7FB8FF",true),
            new ShopItem("vortex_harpoon","Vortex Harpoon","A heavy mechanical bolt that slightly bends surrounding light.",ShopSlot.Arrow,"Tactical & Sci-Fi",400,"#6B7280","#B794F6",true),
            new ShopItem("stealth_phantom","Stealth Phantom","A semi-transparent camouflage arrow that is hard to spot.",ShopSlot.Arrow,"Tactical & Sci-Fi",400,"#9CA3AF","#E5E7EB",true),
            new ShopItem("spore_needle","Spore Needle","An organic wooden arrow dripping with glowing green moss.",ShopSlot.Arrow,"Organic & Strange",450,"#8B5A2B","#7CFF4F",true),
            new ShopItem("bone_carver","Bone Carver","Made entirely of ancient fossils with a jagged edge.",ShopSlot.Arrow,"Organic & Strange",450,"#E8DCC2","#8C7B65",true),
            new ShopItem("abyss_fang","Abyss Fang","A glowing deep-sea creature tooth attached to a coral shaft.",ShopSlot.Arrow,"Organic & Strange",450,"#0E7490","#FF7F50",true),
            new ShopItem("infinity_core","Infinity Core","A reality-bending golden artifact arrow.",ShopSlot.Arrow,"Legendary",1000,"#FFC300","#FFF3B0",true),
            new ShopItem("phoenix_wing","Phoenix Wing","A majestic arrow enveloped completely in bright orange flames.",ShopSlot.Arrow,"Legendary",1000,"#FF7A00","#FFD000",true),
            new ShopItem("chronos_needle","Chronos Needle","A glowing silver arrow that looks like a ticking clock hand.",ShopSlot.Arrow,"Legendary",1000,"#D9E2EC","#7DD3FC",true),
            new ShopItem("mystic_wood","Enchanted Forest","Violet magic glows between deep forest greens.",ShopSlot.Theme,"Themes",200,"#9B5DE5","#0F2418",true),
            new ShopItem("frozen_cavern","Frozen Cavern","Pale ice lights a deep blue cavern.",ShopSlot.Theme,"Themes",250,"#7FDBFF","#0B2540",true),
            new ShopItem("pharaohs_tomb","Pharaoh's Tomb","Warm gold illuminates ancient sandstone shadows.",ShopSlot.Theme,"Themes",350,"#E0B04B","#3B2A14",true),
            new ShopItem("volcanic_underworld","Volcanic Underworld","Molten orange lava glows against dark rock.",ShopSlot.Theme,"Themes",400,"#FF5A1F","#2A0E06",true),
            new ShopItem("neon_cyberpunk","Neon Cyberpunk","Magenta neon shines through midnight city blues.",ShopSlot.Theme,"Themes",500,"#FF2BD6","#0D0221",true),
            new ShopItem("cosmic_nebula","Cosmic Nebula","Violet stardust drifts through deep space.",ShopSlot.Theme,"Themes",800,"#C77DFF","#0B0A24",true),
        });
        /// <summary>Stable display order including the two free defaults.</summary>
        public static IReadOnlyList<ShopItem> Items => items;
        /// <summary>Retired ids stay reserved forever and are never reused for another item.</summary>
        public static IReadOnlyList<string> RetiredItemIds { get; } = Array.AsReadOnly(new[] { "thunderbolt_dart" });
        /// <summary>Returns a known released entry, or null.</summary>
        public static ShopItem Find(string id){for(int i=0;i<items.Count;i++)if(items[i].Id==id)return items[i];return null;}
    }
}
