using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14A regression coverage with isolated state and no player-save access.</summary>
    public sealed class StarRatingTests
    {
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [TestCase(1,2)] [TestCase(2,3)] [TestCase(3,4)] [TestCase(15,20)] [TestCase(30,40)] [TestCase(72,96)]
        public void Boundaries(int par, int limit)
        {
            Assert.That(StarRating.TwoStarLimit(par), Is.EqualTo(limit));
            Assert.That(StarRating.Stars(par,par), Is.EqualTo(3));
            Assert.That(StarRating.Stars(par+1,par), Is.EqualTo(2));
            Assert.That(StarRating.Stars(limit,par), Is.EqualTo(2));
            Assert.That(StarRating.Stars(limit+1,par), Is.EqualTo(1));
            for(int moves=0;moves<=limit+2;moves++) Assert.That(StarRating.StarsIfClearedNow(moves,par), Is.EqualTo(StarRating.Stars(moves,par)));
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(()=>StarRating.Par(null));
            Assert.Throws<ArgumentOutOfRangeException>(()=>StarRating.Stars(-1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>StarRating.Stars(0,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>StarRating.TwoStarLimit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>StarRating.StarsIfClearedNow(-1,2));
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void ParUsesRules()
        {
            var normal=IceTestBoards.NormalCopy(IceTestBoards.Cycle());
            Assert.That(StarRating.Par(normal),Is.EqualTo(4));
            Assert.That(StarRating.Par(IceTestBoards.Cycle()),Is.EqualTo(5));
            var dead=new Board(2,1);dead.TryAddArrow(1,0,0,Direction.Right,ArrowKind.Ice,out _);dead.TryAddArrow(2,1,0,Direction.Left,out _);
            Assert.That(StarRating.Par(dead),Is.EqualTo(3));
        }
    }
}
