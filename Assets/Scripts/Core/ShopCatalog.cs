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
            new ShopItem("avatar_player","Player","The classic neon player badge.",ShopSlot.Avatar,"Classic",0,"#38E1FF","#2A1F86",true),
            new ShopItem("avatar_fox","Swift Fox","A quick-witted fox who always finds the way out.",ShopSlot.Avatar,"Cute Crew",100,"#FF8A3D","#2D1A6E",true),
            new ShopItem("avatar_cat","Neon Cat","A curious violet cat with glowing green eyes.",ShopSlot.Avatar,"Cute Crew",100,"#B79CFF","#1E2A7A",true),
            new ShopItem("avatar_bunny","Bouncy Bunny","A cheerful bunny that hops past every blocker.",ShopSlot.Avatar,"Cute Crew",120,"#FFB3D6","#6A1F78",true),
            new ShopItem("avatar_panda","Bamboo Panda","A calm panda that never rushes a puzzle.",ShopSlot.Avatar,"Cute Crew",150,"#FFFFFF","#16536E",true),
            new ShopItem("avatar_owl","Wise Owl","Sees every arrow's path before it moves.",ShopSlot.Avatar,"Cute Crew",150,"#B07848","#143A5E",true),
            new ShopItem("avatar_ghost","Friendly Ghost","Slips through the maze without a sound.",ShopSlot.Avatar,"Cute Crew",200,"#D6E4FF","#3A1A8A",true),
            new ShopItem("avatar_robot","Circuit Bot","A precise robot with cyan scanner eyes.",ShopSlot.Avatar,"Cosmic Crew",250,"#DCE6F8","#0F2E5E",true),
            new ShopItem("avatar_alien","Star Visitor","A friendly alien from a galaxy of mazes.",ShopSlot.Avatar,"Cosmic Crew",300,"#A8FF7A","#2A0F5E",true),
            new ShopItem("avatar_astronaut","Astronaut","Explores every corner of the puzzle cosmos.",ShopSlot.Avatar,"Cosmic Crew",350,"#9A3CF0","#0B1650",true),
            new ShopItem("avatar_ninja","Shadow Ninja","Silent, fast and never makes a mistake.",ShopSlot.Avatar,"Cosmic Crew",400,"#FF2E4E","#5E0F2A",true),
            new ShopItem("avatar_dragon","Little Dragon","A tiny dragon with a giant puzzle appetite.",ShopSlot.Avatar,"Cosmic Crew",600,"#6AF08A","#5E1A0F",true),
            new ShopItem("frame_bronze","Bronze Ring","A simple bronze ring for every new player.",ShopSlot.Frame,"Classic",0,"#B07A4C","#5A361C",true),
            new ShopItem("frame_winged","Copper Wing","A polished copper ring with a single feathered wing.",ShopSlot.Frame,"Metal Frames",150,"#E0A07A","#7A3E22",true),
            new ShopItem("frame_silver","Silver Arrowhead","Silver crowned with an arrowhead and a laurel shield.",ShopSlot.Frame,"Metal Frames",300,"#F4F8FF","#7C8AA2",true),
            new ShopItem("frame_gold","Royal Gold","Gold laurels set with glowing gems.",ShopSlot.Frame,"Metal Frames",500,"#F2B829","#1AB4D8",true),
            new ShopItem("frame_neon","Neon Orbit","Two neon arrows orbit your badge forever.",ShopSlot.Frame,"Magic Frames",600,"#B85CFF","#4F7BFF",true),
            new ShopItem("frame_runes","Rune Stone","Ancient stone runes held by two crossed arrows.",ShopSlot.Frame,"Magic Frames",700,"#9FF4FF","#5C6870",true),
            new ShopItem("frame_void","Void Rift","Purple void smoke pierced by shadow arrowheads.",ShopSlot.Frame,"Magic Frames",800,"#8A3CF0","#2A0860",true),
            new ShopItem("frame_cosmic","Cosmic Loop","A galaxy swirl wrapped by a rainbow arrow.",ShopSlot.Frame,"Mythic Frames",1000,"#D45CFF","#36C8FF",true),
            new ShopItem("frame_dragon","Shadow Dragon","Dragon scales, horns and burning red eyes.",ShopSlot.Frame,"Mythic Frames",1200,"#6E6E8C","#FF3020",true),
            new ShopItem("frame_venom","Venom Thorns","Toxic green thorns guarding two blood-red gems.",ShopSlot.Frame,"Mythic Frames",1500,"#3CD83A","#B00818",true),
        });
        /// <summary>Stable display order including the four free defaults.</summary>
        public static IReadOnlyList<ShopItem> Items => items;
        /// <summary>Retired ids stay reserved forever and are never reused for another item.</summary>
        public static IReadOnlyList<string> RetiredItemIds { get; } = Array.AsReadOnly(new[] { "thunderbolt_dart" });
        /// <summary>Returns a known released entry, or null.</summary>
        public static ShopItem Find(string id){for(int i=0;i<items.Count;i++)if(items[i].Id==id)return items[i];return null;}
    }
}
