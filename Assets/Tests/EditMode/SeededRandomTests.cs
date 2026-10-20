using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Golden SplitMix64 sequence, rejection bounds and weighted choices.</summary>
    public class SeededRandomTests
    {
        /// <summary>Changing these values deliberately changes every generated board.</summary>
        [Test]public void GoldenAndRepeatable()
        {
            int[] golden={944,597,405,450,363,646,546,68};var a=new SeededRandom(12345);var b=new SeededRandom(12345);
            foreach(int expected in golden){Assert.That(a.NextInt(1000),Is.EqualTo(expected));Assert.That(b.NextInt(1000),Is.EqualTo(expected));}
            for(int i=0;i<1000;i++)Assert.That(a.NextInt(int.MinValue,int.MaxValue),Is.EqualTo(b.NextInt(int.MinValue,int.MaxValue)));
        }
        /// <summary>One hundred thousand draws stay in bounds and ten buckets remain within eight percent.</summary>
        [Test]public void BoundsAndUniformity()
        {
            var random=new SeededRandom(9876);var buckets=new int[10];
            for(int i=0;i<100000;i++){int value=random.NextInt(10);Assert.That(value,Is.InRange(0,9));buckets[value]++;Assert.That(random.NextInt(-7,13),Is.InRange(-7,12));}
            foreach(int count in buckets)Assert.That(count,Is.InRange(9200,10800));
        }
        /// <summary>Zero weights cannot win and all invalid argument cases are explicit.</summary>
        [Test]public void WeightsAndArguments()
        {
            var random=new SeededRandom(3);for(int i=0;i<1000;i++)Assert.That(random.NextWeighted(new[]{0,2,0,5}),Is.EqualTo(1).Or.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextInt(0));Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextInt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextInt(2,2));Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextInt(3,2));
            Assert.Throws<ArgumentNullException>(()=>random.NextWeighted(null));Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextWeighted(new[]{0,0}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>random.NextWeighted(new[]{1,-1}));
        }
    }
}

