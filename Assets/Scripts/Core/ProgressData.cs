using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Version-one progress payload; rules and storage are separate.</summary>
    [System.Serializable]
    public sealed class ProgressData
    {
        /// <summary>Supported save format.</summary>
        public const int CurrentVersion = 1;
        /// <summary>Save format version.</summary>
        public int version = CurrentVersion;
        /// <summary>Most recently started or completed level.</summary>
        public string lastLevelId = "";
        /// <summary>Completion records; normalized by ProgressTracker on load.</summary>
        public List<LevelRecord> levels = new List<LevelRecord>();
    }
}
