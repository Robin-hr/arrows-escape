using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>One generated catalog level beyond the reviewed first sixty.</summary>
    public sealed class CatalogExpansionEntry
    {
        /// <summary>Catalog number (61..360).</summary>
        public int Number { get; }
        /// <summary>Requested step of the Easy / Medium / Hard cycle.</summary>
        public DifficultyTier Tier { get; }
        /// <summary>Board outline.</summary>
        public ShapeKind Shape { get; }
        /// <summary>Mask width.</summary>
        public int Width { get; }
        /// <summary>Mask height.</summary>
        public int Height { get; }
        /// <summary>Generator seed that produced the shipped board.</summary>
        public int Seed { get; }
        /// <summary>Creates an entry.</summary>
        public CatalogExpansionEntry(int number, DifficultyTier tier, ShapeKind shape, int width, int height, int seed)
        { Number = number; Tier = tier; Shape = shape; Width = width; Height = height; Seed = seed; }
        /// <summary>Resource id of the shipped JSON.</summary>
        public string Id => "level_" + Number.ToString("D3") + "_shape_" + Shape.ToString().ToLowerInvariant();
    }
}
