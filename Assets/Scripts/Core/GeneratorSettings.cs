using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Immutable generation request. Validate collects every problem without throwing.</summary>
    public sealed class GeneratorSettings
    {
        /// <summary>Largest generated dimension; phone legibility at this size needs manual review.</summary>
        public const int MaxDimension=10;
        /// <summary>Columns, from 3 through 10.</summary>
        public int Width { get; }
        /// <summary>Rows, from 3 through 10.</summary>
        public int Height { get; }
        /// <summary>Exact final arrow count.</summary>
        public int ArrowCount { get; }
        /// <summary>Exact final Ice count.</summary>
        public int IceCount { get; }
        /// <summary>Requested band, or any tier when null.</summary>
        public DifficultyTier? TargetTier { get; }
        /// <summary>Requires an unsolvable all-Normal copy when Ice is present.</summary>
        public bool RequireSlides { get; }
        /// <summary>Minimum initially free exits.</summary>
        public int MinFreeExitsAtStart { get; }
        /// <summary>Candidate attempts, from 1 through 500.</summary>
        public int MaxAttempts { get; }
        /// <summary>Extra placement weight for blocking existing paths.</summary>
        public int BlockBias { get; }
        /// <summary>Stores a request; validation occurs when generating.</summary>
        public GeneratorSettings(int width=6,int height=6,int arrowCount=14,int iceCount=0,
            DifficultyTier? targetTier=null,bool requireSlides=true,int minFreeExitsAtStart=1,int maxAttempts=40,int blockBias=3)
        {
            Width=width;Height=height;ArrowCount=arrowCount;IceCount=iceCount;TargetTier=targetTier;RequireSlides=requireSlides;
            MinFreeExitsAtStart=minFreeExitsAtStart;MaxAttempts=maxAttempts;BlockBias=blockBias;
        }
        /// <summary>Returns all invalid dimensions, counts, limits and enum values.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors=new List<string>();
            if(Width<3||Width>MaxDimension)errors.Add("Width must be 3..10.");
            if(Height<3||Height>MaxDimension)errors.Add("Height must be 3..10.");
            if(ArrowCount<1||ArrowCount>(long)Width*Height)errors.Add("ArrowCount must be 1..Width*Height.");
            if(IceCount<0||IceCount>ArrowCount)errors.Add("IceCount must be 0..ArrowCount.");
            if(TargetTier.HasValue&&(TargetTier<DifficultyTier.Easy||TargetTier>DifficultyTier.Expert))errors.Add("TargetTier is invalid.");
            if(MinFreeExitsAtStart<0||MinFreeExitsAtStart>ArrowCount)errors.Add("MinFreeExitsAtStart must be 0..ArrowCount.");
            if(MaxAttempts<1||MaxAttempts>500)errors.Add("MaxAttempts must be 1..500.");
            if(BlockBias<0||BlockBias>20)errors.Add("BlockBias must be 0..20.");
            return errors.AsReadOnly();
        }
    }
}
