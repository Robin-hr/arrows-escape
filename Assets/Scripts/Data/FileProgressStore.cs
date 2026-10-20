using System;
using System.IO;
using System.Text;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.Data
{
    /// <summary>Small synchronous versioned JSON store with backups and atomic replacement.</summary>
    public sealed class FileProgressStore : IProgressStore
    {
        private readonly string path;
        private bool preserveOriginal;
        /// <summary>Uses an explicit full path; never looks up an application folder.</summary>
        public FileProgressStore(string fullPath) => path = fullPath;

        /// <summary>Loads versions one and two, migrating stars in memory and backing up corrupt or newer payloads before any future save.</summary>
        public ProgressLoadResult Load()
        {
            preserveOriginal = false;
            try
            {
                // File.Exists alone hides access errors and directories; inspect attributes as well.
                try { File.GetAttributes(path); }
                catch (FileNotFoundException) { return Empty(ProgressLoadStatus.NoFile, "No save file."); }
                catch (DirectoryNotFoundException) { return Empty(ProgressLoadStatus.NoFile, "No save folder."); }
                string json = File.ReadAllText(path);
                ProgressData data;
                try
                {
                    string trimmed = json.Trim();
                    if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) throw new FormatException("Expected a JSON object.");
                    data = new ProgressData { version = 0 };
                    JsonUtility.FromJsonOverwrite(json, data);
                    if (data.version < 1) throw new FormatException("Missing or unsupported save version.");
                }
                catch (Exception error) { return Backup(ProgressLoadStatus.Corrupt, "corrupt", error.Message); }
                if (data.version > ProgressData.CurrentVersion)
                    return Backup(ProgressLoadStatus.NewerVersion, "newer", "Save uses a newer format.");
                if (data.version == 1)
                {
                    if (data.levels != null) foreach (LevelRecord record in data.levels) if (record != null) record.bestStars = 0;
                    data.version = ProgressData.CurrentVersion;
                }
                return new ProgressLoadResult(data, ProgressLoadStatus.Loaded);
            }
            catch (Exception error)
            {
                preserveOriginal = true;
                return Empty(ProgressLoadStatus.ReadError, error.Message);
            }
        }

        /// <summary>Writes a temporary file then moves or atomically replaces the target; failures keep the old file.</summary>
        public bool TrySave(ProgressData data)
        {
            string temporary = path + ".tmp";
            try
            {
                if (data == null || preserveOriginal) return false;
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
                data.version = ProgressData.CurrentVersion;
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception) { return false; }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception) { /* Cleanup must not turn a failed save into an exception. */ }
            }
        }

        private ProgressLoadResult Backup(ProgressLoadStatus status, string suffix, string reason)
        {
            string backup = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + "." + suffix + ".json");
            try { File.Copy(path, backup, true); }
            catch (Exception error)
            {
                preserveOriginal = true;
                return Empty(ProgressLoadStatus.ReadError, reason + " Backup failed: " + backup + ". " + error.Message);
            }
            return Empty(status, reason + " Backup: " + backup);
        }
        private static ProgressLoadResult Empty(ProgressLoadStatus status, string message) => new ProgressLoadResult(null, status, message);
    }
}
