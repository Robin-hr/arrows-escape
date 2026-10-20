using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.Data
{
    /// <summary>Isolated preferences storage for edit-mode bootstrap callers.</summary>
    public sealed class MemorySettingsStore : ISettingsStore
    {
        private string json;
        /// <summary>Returns detached settings or defaults.</summary>
        public SettingsLoadResult Load() => new SettingsLoadResult(json == null ? null : JsonUtility.FromJson<SettingsData>(json), json == null ? ProgressLoadStatus.NoFile : ProgressLoadStatus.Loaded);
        /// <summary>Stores a snapshot in memory only.</summary>
        public bool TrySave(SettingsData data) { if (data == null) return false; data.version = SettingsData.CurrentVersion; json = JsonUtility.ToJson(data); return true; }
    }
}
