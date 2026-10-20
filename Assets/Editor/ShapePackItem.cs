using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>A prepared pack item, retained for review before any write.</summary>
    public sealed class ShapePackItem
    {
        /// <summary>Original slot.</summary>
        public ShapePackEntry Entry { get; }
        /// <summary>Best successful generation, or null.</summary>
        public ShapeGenerationResult Result { get; }
        /// <summary>Measured preparation duration.</summary>
        public long Milliseconds { get; }
        /// <summary>Stores preparation evidence.</summary>
        public ShapePackItem(ShapePackEntry entry,ShapeGenerationResult result,long milliseconds){Entry=entry;Result=result;Milliseconds=milliseconds;}
    }
}
