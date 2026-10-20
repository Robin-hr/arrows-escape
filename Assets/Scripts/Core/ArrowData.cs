using System;

namespace ArrowEscape.Core
{
    /// <summary>Serializable fields for one arrow in a level file.</summary>
    [Serializable]
    public class ArrowData
    {
        /// <summary>Optional flat tail-to-head coordinates; omitted or empty means one cell.</summary>
        public int[] cells;
        /// <summary>The identifier, unique within the level.</summary>
        public int id;
        /// <summary>The column, starting at the left.</summary>
        public int x;
        /// <summary>The row, starting at the bottom.</summary>
        public int y;
        /// <summary>Up, Down, Left, or Right, ignoring case.</summary>
        public string direction;
        /// <summary>Optional Normal or Ice; omitted values preserve normal-arrow levels.</summary>
        public string type;
    }
}
