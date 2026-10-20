using ArrowEscape.Core;
using ArrowEscape.Data;
namespace ArrowEscape.Tests
{
    /// <summary>Counts preference reads and writes without any file access.</summary>
    public sealed class FakeSettingsStore : ISettingsStore
    {
        /// <summary>Saved values, detached by Load and TrySave.</summary>
        public SettingsData Saved;
        /// <summary>Call counters.</summary>
        public int Loads, Saves;
        /// <summary>Simulated failure switch.</summary>
        public bool FailSave;
        /// <summary>Loads defaults or a copy.</summary>
        public SettingsLoadResult Load() { Loads++; return new SettingsLoadResult(Saved == null ? null : Copy(Saved), Saved == null ? ProgressLoadStatus.NoFile : ProgressLoadStatus.Loaded); }
        /// <summary>Counts and optionally stores the changed values.</summary>
        public bool TrySave(SettingsData data) { Saves++; if (FailSave) return false; Saved = Copy(data); return true; }
        private static SettingsData Copy(SettingsData data) => new SettingsData { version = data.version, soundEnabled = data.soundEnabled, hapticsEnabled = data.hapticsEnabled };
    }
}
