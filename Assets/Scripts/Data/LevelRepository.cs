using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.Data
{
    /// <summary>Loads JSON levels from Unity Resources.</summary>
    public static class LevelRepository
    {
        /// <summary>Loads, parses, and builds a level; the path excludes its file extension.</summary>
        public static LevelLoadResult LoadFromResources(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return LevelLoadResult.Fail(new[] { "Level resource path is null or empty." });
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                return LevelLoadResult.Fail(new[] { $"Level resource '{resourcePath}' was not found." });
            if (!LevelJson.TryParse(asset.text, out LevelData data, out string error))
                return LevelLoadResult.Fail(new[] { $"Level resource '{resourcePath}': {error}" });
            return LevelBuilder.Build(data);
        }

        /// <summary>Lists the Journey levels in Resources/JourneyLevels, sorted by name.</summary>
        public static IReadOnlyList<string> GetJourneyResourcePaths()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("JourneyLevels");
            Array.Sort(assets, (left, right) => string.CompareOrdinal(left.name, right.name));
            var paths = new List<string>();
            foreach (TextAsset asset in assets) paths.Add("JourneyLevels/" + asset.name);
            return paths.AsReadOnly();
        }
        /// <summary>Lists TextAssets directly in Resources/Levels, sorted by name.</summary>
        public static IReadOnlyList<string> GetLevelResourcePaths()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("Levels");
            Array.Sort(assets, (left, right) => string.CompareOrdinal(left.name, right.name));
            var paths = new List<string>();
            foreach (TextAsset asset in assets) paths.Add("Levels/" + asset.name);
            return paths.AsReadOnly();
        }
    }
}
