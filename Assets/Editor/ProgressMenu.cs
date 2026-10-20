using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArrowEscape.Editor
{
    /// <summary>Explicit editor-only helpers for locating and deleting local progress.</summary>
    public static class ProgressMenu
    {
        [MenuItem("ArrowEscape/Reveal Save Folder")]
        private static void Reveal()
        {
            string file = Path.Combine(Application.persistentDataPath, "progress.json");
            EditorUtility.RevealInFinder(File.Exists(file) ? file : Application.persistentDataPath);
        }
        [MenuItem("ArrowEscape/Delete Save File")]
        private static void Delete()
        {
            if (!EditorUtility.DisplayDialog("Delete Arrow Escape progress?",
                "Delete progress.json and its backups? This does not affect an open Play session's in-memory progress. Stop Play first to reset the next launch.", "Delete", "Cancel")) return;
            try
            {
                string folder = Application.persistentDataPath;
                foreach (string name in new[] { "progress.json", "progress.corrupt.json", "progress.newer.json", "progress.json.tmp" })
                {
                    string file = Path.Combine(folder, name);
                    if (File.Exists(file)) File.Delete(file);
                }
            }
            catch (Exception error) { Debug.LogWarning("ArrowEscape could not delete progress: " + error.Message); }
        }
    }
}
