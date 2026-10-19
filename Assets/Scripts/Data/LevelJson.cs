using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.Data
{
    /// <summary>Parses JSON text using Unity's built-in serializer.</summary>
    public static class LevelJson
    {
        /// <summary>Returns data or a clear parsing error without throwing.</summary>
        public static bool TryParse(string json, out LevelData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Level JSON text is null or empty.";
                return false;
            }
            string text = json.Trim();
            if (!text.StartsWith("{", StringComparison.Ordinal) ||
                !text.EndsWith("}", StringComparison.Ordinal))
            {
                error = "Malformed level JSON: expected an object enclosed in braces.";
                return false;
            }
            try
            {
                data = JsonUtility.FromJson<LevelData>(text);
                if (data != null) return true;
                error = "Level JSON did not contain a level object.";
            }
            catch (Exception exception)
            {
                error = $"Malformed level JSON: {exception.Message}";
            }
            data = null;
            return false;
        }
    }
}
