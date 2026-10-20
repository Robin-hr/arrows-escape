using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks deterministic stratification, bounds and invalid settings.</summary>
    public class BurstPlannerTests
    {
        /// <summary>A reusable valid configuration for effect tests.</summary>
        public static BurstSettings Settings(int count = 10) => new BurstSettings { Count=count,ConeHalfAngleDegrees=180,MinDistance=.6f,MaxDistance=1.3f,MinLifetime=.3f,MaxLifetime=.5f,StartSize=.16f,EndSize=.03f,ColorCount=5 };
        /// <summary>Count respects the buffer and empty requests.</summary>
        [Test] public void Counts()
        {
            var buffer = new ParticleSpec[4]; Assert.That(BurstPlanner.Plan(1,Settings(),buffer),Is.EqualTo(4));
            Assert.That(BurstPlanner.Plan(1,Settings(0),buffer),Is.Zero); Assert.That(BurstPlanner.Plan(1,Settings(-2),buffer),Is.Zero);
            Assert.Throws<ArgumentNullException>(() => BurstPlanner.Plan(1,Settings(),null));
        }
        /// <summary>Every requested invalid setting is rejected.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void Invalid(int kind)
        {
            var s=Settings(); switch(kind) { case 0:s.MinDistance=5;break;case 1:s.MinLifetime=5;break;case 2:s.ConeHalfAngleDegrees=-1;break;case 3:s.ConeHalfAngleDegrees=181;break;case 4:s.ColorCount=0;break;case 5:s.StartSize=0;break;case 6:s.EndSize=-1;break; }
            Assert.Throws<ArgumentOutOfRangeException>(()=>BurstPlanner.Plan(1,s,new ParticleSpec[10]));
        }
        /// <summary>Ten full-circle particles occupy distinct 36-degree slices, with unit vectors.</summary>
        [Test] public void Stratified()
        {
            var buffer=new ParticleSpec[10]; BurstPlanner.Plan(17,Settings(),buffer); var slices=new int[10];
            for(int i=0;i<buffer.Length;i++) {
                var p=buffer[i]; double angle=Math.Atan2(p.DirY,p.DirX)*180/Math.PI;
                int slice=Math.Min(9,(int)Math.Floor((angle+180)/36)); slices[slice]++;
                Assert.That(Math.Sqrt(p.DirX*p.DirX+p.DirY*p.DirY),Is.EqualTo(1).Within(.0001));
                Assert.That(p.Distance,Is.InRange(.6f,1.3f));Assert.That(p.Lifetime,Is.InRange(.3f,.5f));Assert.That(p.ColorIndex,Is.EqualTo(i%5));
            }
            Assert.That(slices,Is.All.EqualTo(1));
        }
        /// <summary>Wrapped cones stay within their angular bounds and Up points upward.</summary>
        [TestCase(Direction.Right,0)][TestCase(Direction.Up,90)][TestCase(Direction.Left,180)][TestCase(Direction.Down,270)]
        public void DirectionCones(Direction direction,float degrees)
        {
            Assert.That(BurstPlanner.AngleFor(direction),Is.EqualTo(degrees));var s=Settings(100);s.DirectionDegrees=degrees;s.ConeHalfAngleDegrees=55;var b=new ParticleSpec[100];BurstPlanner.Plan(71,s,b);
            foreach(var p in b) { double angle=Math.Atan2(p.DirY,p.DirX)*180/Math.PI;double delta=(angle-degrees+540)%360-180;Assert.That(Math.Abs(delta),Is.LessThanOrEqualTo(55.0001));if(direction==Direction.Up)Assert.That(p.DirY,Is.GreaterThan(0)); }
        }
        /// <summary>Seeded plans are exactly repeatable and a new seed changes the plan.</summary>
        [Test] public void Deterministic()
        {var a=new ParticleSpec[10];var b=new ParticleSpec[10];BurstPlanner.Plan(7,Settings(),a);BurstPlanner.Plan(7,Settings(),b);Assert.That(b,Is.EqualTo(a));BurstPlanner.Plan(8,Settings(),b);Assert.That(b,Is.Not.EqualTo(a));}
    }
}
