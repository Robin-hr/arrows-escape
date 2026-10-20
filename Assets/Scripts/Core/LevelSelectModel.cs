using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Builds safe display snapshots from ordered ids and progress.</summary>
    public static class LevelSelectModel
    {
        /// <summary>Returns no tiles for missing ids; null progress means a fresh player.</summary>
        public static IReadOnlyList<LevelTile> Build(IReadOnlyList<string> orderedLevelIds, ProgressTracker tracker)
        {
            var tiles = new List<LevelTile>();
            if (orderedLevelIds == null) return tiles.AsReadOnly();
            tracker = tracker ?? new ProgressTracker();
            int resume = tracker.GetResumeIndex(orderedLevelIds);
            for (int i = 0; i < orderedLevelIds.Count; i++)
            {
                string id = orderedLevelIds[i] ?? "";
                var state = tracker.IsCompleted(id) ? LevelTileState.Completed : tracker.IsUnlocked(orderedLevelIds, i) ? LevelTileState.Unlocked : LevelTileState.Locked;
                bool found = tracker.TryGetRecord(id, out LevelRecord record);
                tiles.Add(new LevelTile(i, id, state, i == resume, found ? record.bestMistakes : -1, found ? record.bestUndos : -1, found ? record.bestStars : 0));
            }
            return tiles.AsReadOnly();
        }
    }
}
