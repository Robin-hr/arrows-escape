using System;
namespace ArrowEscape.Core
{
    /// <summary>Deterministic lightning and ambient decisions, independent of engine randomness.</summary>
    public static class SkinEffectRules
    {
        /// <summary>At most one ambient start each four tenths of a second.</summary>
        public const float AmbientInterval=.4f;
        /// <summary>Hard budget shared by every pooled particle system.</summary>
        public const int ParticleLimit=600;
        /// <summary>Stable seed-based spark offset for an integer segment and time frame.</summary>
        public static int Lightning(int seed,int segment,int frame)
        {uint v=unchecked((uint)seed*0x9e3779b9u+(uint)segment*0x85ebca6bu+(uint)frame*0xc2b2ae35u);v^=v>>16;v*=0x7feb352du;v^=v>>15;return (int)(v%1001)-500;}
    }
}
