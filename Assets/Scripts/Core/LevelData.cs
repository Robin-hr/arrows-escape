using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Serializable level fields matching the JSON file format.</summary>
    [Serializable]
    public class LevelData
    {
        /// <summary>The display name.</summary>
        public string name;
        /// <summary>The number of columns (1 to 64).</summary>
        public int width;
        /// <summary>The number of rows (1 to 64).</summary>
        public int height;
        /// <summary>The arrows in insertion order.</summary>
        public List<ArrowData> arrows;
    }
}
