using System;
namespace ArrowEscape.Core
{
    /// <summary>An immutable replayable exit or slide; target coordinates matter only for slides.</summary>
    public readonly struct SolverMove : IEquatable<SolverMove>
    {
        /// <summary>Stable identity of the arrow to tap.</summary>
        public int ArrowId { get; }
        /// <summary>Whether the tap must slide rather than exit.</summary>
        public bool IsSlide { get; }
        /// <summary>Slide destination column.</summary>
        public int ToX { get; }
        /// <summary>Slide destination row.</summary>
        public int ToY { get; }
        /// <summary>Describes one legal tap.</summary>
        public SolverMove(int arrowId,bool isSlide,int toX=0,int toY=0) { ArrowId=arrowId;IsSlide=isSlide;ToX=toX;ToY=toY; }
        /// <summary>Compares a complete replay instruction.</summary>
        public bool Equals(SolverMove other)=>ArrowId==other.ArrowId&&IsSlide==other.IsSlide&&ToX==other.ToX&&ToY==other.ToY;
        /// <summary>Compares boxed instructions.</summary>
        public override bool Equals(object obj)=>obj is SolverMove move&&Equals(move);
        /// <summary>Hashes all replay fields.</summary>
        public override int GetHashCode()=>((ArrowId*397^(IsSlide?1:0))*397^ToX)*397^ToY;
    }
}
