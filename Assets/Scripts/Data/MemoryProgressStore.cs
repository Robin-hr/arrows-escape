using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.Data
{
    /// <summary>Isolated volatile storage for edit-mode callers without an injected disk store.</summary>
    public sealed class MemoryProgressStore : IProgressStore
    {
        private string saved;
        /// <summary>Returns a detached payload, or NoFile before the first save.</summary>
        public ProgressLoadResult Load() => saved == null
            ? new ProgressLoadResult(null, ProgressLoadStatus.NoFile)
            : new ProgressLoadResult(JsonUtility.FromJson<ProgressData>(saved), ProgressLoadStatus.Loaded);
        /// <summary>Stores only an in-memory JSON snapshot.</summary>
        public bool TrySave(ProgressData data)
        {
            if (data == null) return false;
            data.version = ProgressData.CurrentVersion;
            saved = JsonUtility.ToJson(data);
            return true;
        }
    }
}
