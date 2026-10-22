using System;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Removes retired Inferno art dependencies and retains an explicit player shader reference.</summary>
    public static class InfernoSetup
    {
        /// <summary>Idempotent setup; never reads the visual target or edits other skins.</summary>
        public static void Configure()
        {
            var resources=AssetDatabase.LoadAssetAtPath<ArrowSkinResources>("Assets/Resources/ArrowSkinResources.asset");
            resources.Inferno=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/InfernoProcedural.shader");
            resources.RareTheme=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/RareThemeProcedural.shader");
            resources.Elemental=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/ElementalProcedural.shader");
            resources.Art=Array.FindAll(resources.Art,art=>art.Id!="inferno_streak"&&art.Id!="frostbite"&&art.Id!="plasma_bolt"&&art.Id!="void_whisper"&&art.Id!="thunderbolt_dart"&&art.Id!="obsidian_shard"&&art.Id!="chromium_spike"&&art.Id!="celestial_quill"&&art.Id!="emerald_viper"&&art.Id!="crimson_relic");EditorUtility.SetDirty(resources);AssetDatabase.SaveAssets();
            if(AssetDatabase.IsValidFolder("Assets/Art/Arrows/inferno_streak"))AssetDatabase.DeleteAsset("Assets/Art/Arrows/inferno_streak");
            foreach(string id in new[]{"frostbite","plasma_bolt","void_whisper","thunderbolt_dart","obsidian_shard","chromium_spike","celestial_quill","emerald_viper","crimson_relic"})if(AssetDatabase.IsValidFolder("Assets/Art/Arrows/"+id))AssetDatabase.DeleteAsset("Assets/Art/Arrows/"+id);
            AssetDatabase.Refresh();
        }
    }
}
