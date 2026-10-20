using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>One immutable requested shape-pack slot.</summary>
    public sealed class ShapePackEntry
    {
        /// <summary>Ordered catalog number beginning at four.</summary>
        public int Number { get; }
        /// <summary>Shape to generate.</summary>
        public ShapeKind Shape { get; }
        /// <summary>Requested width.</summary>
        public int Width { get; }
        /// <summary>Requested height.</summary>
        public int Height { get; }
        /// <summary>First of fifty permitted seeds.</summary>
        public int BaseSeed { get; }
        /// <summary>Legacy custom-entry tier; the area pack does not request a tier.</summary>
        public DifficultyTier Tier { get; }
        /// <summary>Requested inside-cell band; zero for legacy custom entries.</summary>
        public int TargetArea { get; }
        /// <summary>Stable filename and progress id.</summary>
        public string Id=>"level_"+Number.ToString("D3",System.Globalization.CultureInfo.InvariantCulture)+"_shape_"+Shape.ToString().ToLowerInvariant();
        /// <summary>Creates one plan slot.</summary>
        public ShapePackEntry(int number,ShapeKind shape,int width,int height,int seed,DifficultyTier tier=DifficultyTier.Easy,int targetArea=0)
        {Number=number;Shape=shape;Width=width;Height=height;BaseSeed=seed;Tier=tier;TargetArea=targetArea;}
    }
}
