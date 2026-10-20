using System;
using ArrowEscape.Core;

namespace ArrowEscape.Data
{
    /// <summary>Coordinates rules and storage; failed saves never roll back in-memory progress.</summary>
    public sealed class ProgressService
    {
        private readonly IProgressStore store;
        /// <summary>Current in-memory progress rules.</summary>
        public ProgressTracker Tracker { get; private set; } = new ProgressTracker();
        /// <summary>Most recent load diagnostic.</summary>
        public ProgressLoadResult LoadResult { get; private set; }
        /// <summary>Whether the latest save failed.</summary>
        public bool LastSaveFailed { get; private set; }
        /// <summary>Uses an injected store; null is an error.</summary>
        public ProgressService(IProgressStore store) => this.store = store ?? throw new ArgumentNullException(nameof(store));
        /// <summary>Reads the store and sanitizes the resulting payload.</summary>
        public void Load()
        {
            try { LoadResult = store.Load() ?? new ProgressLoadResult(null, ProgressLoadStatus.ReadError, "Store returned no result."); }
            catch (Exception error) { LoadResult = new ProgressLoadResult(null, ProgressLoadStatus.ReadError, error.Message); }
            Tracker = new ProgressTracker(LoadResult.Data);
            LastSaveFailed = false;
        }
        /// <summary>Records a completion, remembers its id, and attempts one save.</summary>
        public CompletionResult RecordCompletion(string levelId, int mistakes, int undos)
        {
            CompletionResult result = Tracker.RecordCompletion(levelId, mistakes, undos);
            Tracker.SetLastLevel(levelId);
            Save();
            return result;
        }
        /// <summary>Remembers a level start and attempts one save without saving an attempt.</summary>
        public void RememberLevel(string levelId) { Tracker.SetLastLevel(levelId); Save(); }
        /// <summary>Clears progress and attempts one save.</summary>
        public void ResetAll() { Tracker.Reset(); Save(); }
        private void Save()
        {
            try { LastSaveFailed = !store.TrySave(Tracker.Data); }
            catch (Exception) { LastSaveFailed = true; }
        }
    }
}
