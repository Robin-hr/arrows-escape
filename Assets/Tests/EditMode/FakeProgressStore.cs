using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Counts calls and snapshots data without touching any application folder.</summary>
    public sealed class FakeProgressStore : IProgressStore
    {
        /// <summary>Payload returned on load, or null to simulate first launch.</summary>
        public ProgressData Saved;
        /// <summary>Number of save requests.</summary>
        public int Saves;
        /// <summary>Number of load requests.</summary>
        public int Loads;
        /// <summary>Simulates a failed write.</summary>
        public bool FailSave;
        /// <summary>Optional diagnostic returned by Load.</summary>
        public ProgressLoadResult Result;
        /// <summary>Returns a detached payload.</summary>
        public ProgressLoadResult Load()
        {
            Loads++;
            return Result ?? new ProgressLoadResult(Saved == null ? null : Copy(Saved), Saved == null ? ProgressLoadStatus.NoFile : ProgressLoadStatus.Loaded);
        }
        /// <summary>Counts writes and stores a detached snapshot on success.</summary>
        public bool TrySave(ProgressData data) { Saves++; if (FailSave) return false; Saved = Copy(data); return true; }
        private static ProgressData Copy(ProgressData data) => JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(data));
    }
}
