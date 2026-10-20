using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable requested box and quality controls for Normal path generation.</summary>
    public sealed class ShapeLevelSettings
    {
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
        /// <summary>Optional desired tier.</summary>
        public DifficultyTier? TargetTier { get; }
        /// <summary>Stores settings; Validate collects every invalid value.</summary>
        public ShapeLevelSettings(ShapeKind shape,int width,int height,int minArrowLength=2,int? maxArrowLength=null,int turnBias=45,int coverageTargetPermille=900,int maxFreeAtStartPermille=300,int maxAttempts=30,DifficultyTier? targetTier=null)
        {Shape=shape;Width=width;Height=height;MinArrowLength=minArrowLength;MaxArrowLength=maxArrowLength??Math.Min(12,Math.Max(4,width/2));TurnBias=turnBias;CoverageTargetPermille=coverageTargetPermille;MaxFreeAtStartPermille=maxFreeAtStartPermille;MaxAttempts=maxAttempts;TargetTier=targetTier;}
        /// <summary>Returns every invalid setting without throwing.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors=new List<string>();int min=ShapeMask.MinimumSize(Shape);
            if(Shape<ShapeKind.Ellipse||Shape>ShapeKind.Decagon)errors.Add("Unknown shape.");
            if(Width<min||Width>64)errors.Add("Width must be between the shape minimum "+min+" and 64.");
            if(Height<min||Height>64)errors.Add("Height must be between the shape minimum "+min+" and 64.");
            if(MinArrowLength<2)errors.Add("MinArrowLength must be at least 2.");
            if(MaxArrowLength<MinArrowLength||MaxArrowLength>4096)errors.Add("MaxArrowLength must be at least MinArrowLength and at most 4096.");
            if(TurnBias<0||TurnBias>100)errors.Add("TurnBias must be 0..100.");
            if(CoverageTargetPermille<1||CoverageTargetPermille>1000)errors.Add("CoverageTargetPermille must be 1..1000.");
            if(MaxFreeAtStartPermille<1||MaxFreeAtStartPermille>1000)errors.Add("MaxFreeAtStartPermille must be 1..1000.");
            if(MaxAttempts<1||MaxAttempts>500)errors.Add("MaxAttempts must be 1..500.");
            if(TargetTier.HasValue&&(TargetTier<DifficultyTier.Easy||TargetTier>DifficultyTier.Expert))errors.Add("Unknown target tier.");
            return errors.AsReadOnly();
        }
    }
}
