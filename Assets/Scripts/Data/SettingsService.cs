using System;
using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Loads preferences and saves only real changes, preserving values on failure.</summary>
    public sealed class SettingsService
    {
        private readonly ISettingsStore store;
        /// <summary>Current observable values.</summary>
        public SettingsModel Model { get; private set; } = new SettingsModel();
        /// <summary>Most recent load diagnostic.</summary>
        public SettingsLoadResult LoadResult { get; private set; }
        /// <summary>Whether the most recent changed value failed to save.</summary>
        public bool LastSaveFailed { get; private set; }
        /// <summary>Uses an injected store; null is a programming error.</summary>
        public SettingsService(ISettingsStore store) => this.store = store ?? throw new ArgumentNullException(nameof(store));
        /// <summary>Loads safely, defaulting on a store exception.</summary>
        public void Load()
        {
            try { LoadResult = store.Load() ?? new SettingsLoadResult(null, ProgressLoadStatus.ReadError, "Store returned no result."); }
            catch (Exception error) { LoadResult = new SettingsLoadResult(null, ProgressLoadStatus.ReadError, error.Message); }
            Model = new SettingsModel(LoadResult.Data); LastSaveFailed = false;
        }
        /// <summary>Sets and saves sound only if changed.</summary>
        public void SetSound(bool value) { if (Model.SoundEnabled == value) return; Model.SoundEnabled = value; Save(); }
        /// <summary>Sets and saves haptics only if changed.</summary>
        public void SetHaptics(bool value) { if (Model.HapticsEnabled == value) return; Model.HapticsEnabled = value; Save(); }
        private void Save() { try { LastSaveFailed = !store.TrySave(Model.Data); } catch (Exception) { LastSaveFailed = true; } }
    }
}
