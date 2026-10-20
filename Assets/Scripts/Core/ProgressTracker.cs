using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Pure completion, best-attempt, unlock, and resume rules.</summary>
    public sealed class ProgressTracker
    {
        /// <summary>The normalized payload for saving.</summary>
        public ProgressData Data { get; }
        /// <summary>Wraps and sanitizes a supplied payload or creates empty progress.</summary>
        public ProgressTracker(ProgressData data = null) { Data = data ?? new ProgressData(); Sanitize(); }

        /// <summary>Extracts a stable id from either slash style and an optional extension.</summary>
        public static string ToLevelId(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return "";
            int slash = Math.Max(resourcePath.LastIndexOf('/'), resourcePath.LastIndexOf('\\'));
            string name = resourcePath.Substring(slash + 1);
            int dot = name.LastIndexOf('.');
            return dot < 0 ? name : name.Substring(0, dot);
        }

        /// <summary>Records one clear, clamping negative scores and comparing the whole attempt.</summary>
        public CompletionResult RecordCompletion(string levelId, int mistakes, int undos)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            mistakes = Math.Max(0, mistakes); undos = Math.Max(0, undos);
            LevelRecord record = Find(levelId);
            bool first = record == null || record.completions == 0;
            if (record == null) { record = new LevelRecord { id = levelId }; Data.levels.Add(record); }
            bool better = first || Better(mistakes, undos, record);
            if (better) { record.bestMistakes = mistakes; record.bestUndos = undos; }
            record.completions = Sum(record.completions, 1);
            return new CompletionResult(better, first, record.bestMistakes, record.bestUndos, record.completions);
        }

        /// <summary>Sum of saved best stars, including records outside the current catalog.</summary>
        public int TotalStars { get { int total = 0; foreach (LevelRecord record in Data.levels) total = Sum(total, record.bestStars); return total; } }
        /// <summary>Records a star result alongside the unchanged best-mistakes and best-undos rules.</summary>
        public CompletionResult RecordCompletion(string levelId, int mistakes, int undos, int moves, int par)
        {
            int stars = StarRating.Stars(moves, par);
            int previous = Find(levelId)?.bestStars ?? 0;
            CompletionResult result = RecordCompletion(levelId, mistakes, undos);
            LevelRecord record = Find(levelId);
            record.bestStars = Math.Max(previous, stars);
            return new CompletionResult(result, stars, moves, par, record.bestStars, stars > previous);
        }
        /// <summary>Whether this id has at least one recorded clear.</summary>
        public bool IsCompleted(string levelId) => Find(levelId)?.completions > 0;
        /// <summary>Returns a detached copy so callers cannot change the stored best.</summary>
        public bool TryGetRecord(string levelId, out LevelRecord record)
        {
            LevelRecord found = Find(levelId);
            record = found == null ? null : Copy(found);
            return record != null;
        }
        /// <summary>Only the first level or a level whose immediate predecessor is completed is unlocked.</summary>
        public bool IsUnlocked(IReadOnlyList<string> orderedLevelIds, int index) =>
            orderedLevelIds != null && index >= 0 && index < orderedLevelIds.Count &&
            (index == 0 || IsCompleted(orderedLevelIds[index - 1]));
        /// <summary>Resumes the first incomplete level; all complete resumes the last, no levels returns -1.</summary>
        public int GetResumeIndex(IReadOnlyList<string> orderedLevelIds)
        {
            if (orderedLevelIds == null || orderedLevelIds.Count == 0) return -1;
            for (int i = 0; i < orderedLevelIds.Count; i++) if (!IsCompleted(orderedLevelIds[i])) return i;
            return orderedLevelIds.Count - 1;
        }
        /// <summary>Counts completions only among ids in the current ordered list.</summary>
        public int CompletedCount(IReadOnlyList<string> orderedLevelIds)
        {
            int count = 0;
            if (orderedLevelIds != null) foreach (string id in orderedLevelIds) if (IsCompleted(id)) count++;
            return count;
        }
        /// <summary>Remembers the last level without changing completion or unlock state.</summary>
        public void SetLastLevel(string levelId) => Data.lastLevelId = levelId ?? "";
        /// <summary>Clears all records and the remembered level.</summary>
        public void Reset() { Data.levels.Clear(); Data.lastLevelId = ""; Data.version = ProgressData.CurrentVersion; }

        private LevelRecord Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (LevelRecord record in Data.levels) if (record.id == id) return record;
            return null;
        }
        private void Sanitize()
        {
            var clean = new List<LevelRecord>();
            if (Data.levels != null)
                foreach (LevelRecord record in Data.levels)
                {
                    if (record == null || string.IsNullOrWhiteSpace(record.id) || record.completions < 0 ||
                        record.bestMistakes < 0 || record.bestUndos < 0) continue;
                    LevelRecord existing = null;
                    foreach (LevelRecord item in clean) if (item.id == record.id) { existing = item; break; }
                    if (existing == null) clean.Add(Copy(record));
                    else
                    {
                        if (Better(record.bestMistakes, record.bestUndos, existing))
                        { existing.bestMistakes = record.bestMistakes; existing.bestUndos = record.bestUndos; }
                        existing.completions = Sum(existing.completions, record.completions);
                        existing.bestStars = Math.Max(existing.bestStars, Math.Max(0, Math.Min(3, record.bestStars)));
                    }
                }
            Data.levels = clean;
            Data.lastLevelId = Data.lastLevelId ?? "";
        }
        private static bool Better(int mistakes, int undos, LevelRecord current) =>
            mistakes < current.bestMistakes || (mistakes == current.bestMistakes && undos < current.bestUndos);
        private static int Sum(int a, int b) => (int)Math.Min(int.MaxValue, (long)a + b);
        private static LevelRecord Copy(LevelRecord record) => new LevelRecord
        { id = record.id, completions = record.completions, bestMistakes = record.bestMistakes, bestUndos = record.bestUndos, bestStars = Math.Max(0, Math.Min(3, record.bestStars)) };
    }
}
