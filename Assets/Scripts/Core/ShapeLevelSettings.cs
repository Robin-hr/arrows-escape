using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable requested box and quality controls for Normal path generation.</summary>
    public sealed class ShapeLevelSettings
    {
        /// <summary>Catalog paths always contain at least four cells.</summary>
        public const int CatalogMinArrowLength=4;
        /// <summary>Catalog paths never exceed ten cells, including extended tails.</summary>
        public const int CatalogMaxArrowLength=10;
        /// <summary>Explicit small starter authoring exception to mask minimum sizes.</summary>
        public bool IsStarter { get; }
        /// <summary>Maximum turns in an authored starter.</summary>
        public int MaxBends { get; }
        /// <summary>Minimum accepted arrow count.</summary>
        public int MinArrows { get; }
        /// <summary>Maximum accepted arrow count.</summary>
        public int MaxArrows { get; }
        /// <summary>Minimum initially free arrows.</summary>
        public int MinFree { get; }
        /// <summary>Mask kind.</summary>
        public ShapeKind Shape { get; }
        /// <summary>Requested box width.</summary>
        public int Width { get; }
        /// <summary>Requested box height.</summary>
        public int Height { get; }
        /// <summary>Minimum cells per path.</summary>
        public int MinArrowLength { get; }
        /// <summary>Maximum cells per path including tail extension.</summary>
        public int MaxArrowLength { get; }
        /// <summary>Turn preference percent.</summary>
        public int TurnBias { get; }
        /// <summary>Required covered mask cells per thousand.</summary>
        public int CoverageTargetPermille { get; }
        /// <summary>Maximum initially free arrows per thousand.</summary>
        public int MaxFreeAtStartPermille { get; }
        /// <summary>Maximum candidate attempts.</summary>
        public int MaxAttempts { get; }
        /// <summary>Journey mode (Step 14L): arrows may reach <see cref="MaxArrowLength"/> (up to 16) instead of the catalog cap of 10, with uniform target lengths.</summary>
        public bool LongArrows { get; }
        /// <summary>Longest arrow the generator grows.</summary>
        public int LengthCap => LongArrows ? Math.Min(MaxArrowLength, 16) : Math.Min(MaxArrowLength, 10);
        /// <summary>Optional desired tier.</summary>
        public DifficultyTier? TargetTier { get; }
        /// <summary>Stores settings; Validate collects every invalid value.</summary>
        public ShapeLevelSettings(ShapeKind shape,int width,int height,int minArrowLength=4,int? maxArrowLength=null,int turnBias=45,int coverageTargetPermille=900,int maxFreeAtStartPermille=300,int maxAttempts=30,DifficultyTier? targetTier=null,bool starter=false,int maxBends=int.MaxValue,int minArrows=1,int maxArrows=int.MaxValue,int minFree=1,bool longArrows=false)
        {LongArrows=longArrows;Shape=shape;Width=width;Height=height;MinArrowLength=minArrowLength;MaxArrowLength=maxArrowLength??10;TurnBias=turnBias;CoverageTargetPermille=coverageTargetPermille;MaxFreeAtStartPermille=maxFreeAtStartPermille;MaxAttempts=maxAttempts;TargetTier=targetTier;IsStarter=starter;MaxBends=maxBends;MinArrows=minArrows;MaxArrows=maxArrows;MinFree=minFree;}
        /// <summary>Returns every invalid setting without throwing.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors=new List<string>();int min=IsStarter?3:ShapeMask.MinimumSize(Shape);
            if(Shape<ShapeKind.Ellipse||Shape>ShapeKind.Decagon)errors.Add("Unknown shape.");
            if(Width<min||Width>64)errors.Add("Width must be between the shape minimum "+min+" and 64.");
            if(Height<min||Height>64)errors.Add("Height must be between the shape minimum "+min+" and 64.");
            if(MinArrowLength<CatalogMinArrowLength)errors.Add("MinArrowLength must be at least 4.");
            if(MaxArrowLength<MinArrowLength||MaxArrowLength>4096)errors.Add("MaxArrowLength must be at least MinArrowLength and at most 4096.");
            if(TurnBias<0||TurnBias>100)errors.Add("TurnBias must be 0..100.");
            if(CoverageTargetPermille<1||CoverageTargetPermille>1000)errors.Add("CoverageTargetPermille must be 1..1000.");
            if(MaxFreeAtStartPermille<1||MaxFreeAtStartPermille>1000)errors.Add("MaxFreeAtStartPermille must be 1..1000.");
            if(MaxAttempts<1||MaxAttempts>500)errors.Add("MaxAttempts must be 1..500.");
            if(TargetTier.HasValue&&(TargetTier<DifficultyTier.Easy||TargetTier>DifficultyTier.Expert))errors.Add("Unknown target tier.");
            if(MaxBends<0)errors.Add("MaxBends must be nonnegative.");
            if(MinArrows<1||MaxArrows<MinArrows)errors.Add("Arrow-count bounds must be positive and ordered.");
            if(MinFree<1)errors.Add("MinFree must be positive.");
            return errors.AsReadOnly();
        }
    }
}
