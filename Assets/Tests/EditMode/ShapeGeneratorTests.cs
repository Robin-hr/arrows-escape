using System;
using System.Linq;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks real generated paths and the quality contract over fixed seed ranges.</summary>
    public class ShapeGeneratorTests
    {
        private static string Key(Board b)=>string.Join(";",b.Arrows.Select(a=>a.Id+":"+a.Direction+":"+string.Join(",",a.Cells.Select(c=>c.X+"/"+c.Y))));
        /// <summary>Same seed, settings and retries produce the same path identities.</summary>
        [Test]public void Determinism(){var s=new ShapeLevelSettings(ShapeKind.Square,8,8);var a=ShapeLevelGenerator.Generate(s,1);Assert.That(a.Success,Is.True);for(int i=0;i<2;i++)Assert.That(Key(ShapeLevelGenerator.Generate(s,1).Board),Is.EqualTo(Key(a.Board)));}
        /// <summary>All successful candidates satisfy every filter, including post-extension solvability.</summary>
        [Test, Timeout(1800000)]public void AllShapesThreeSizesTenSeeds()
        {
            int successes=0;foreach(ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))for(int round=0;round<3;round++)for(int seed=0;seed<10;seed++)
            {
                var size=ShapeMask.DefaultSize(kind,ShapeMask.MinimumSize(kind)+6*round);var s=new ShapeLevelSettings(kind,size.X,size.Y);var r=ShapeLevelGenerator.Generate(s,seed);if(!r.Success){Assert.That(r.FailureReason,Is.Not.Empty);continue;}successes++;Check(r,s);
            }
            Assert.That(successes,Is.GreaterThan(0));TestContext.WriteLine("Passing candidates: "+successes+" / 570");
        }
        private static void Check(ShapeGenerationResult r,ShapeLevelSettings s)
        {
            Board b=r.Board;Assert.That(LevelSolvability.IsSolvable(b),Is.True);int occupied=0;
            foreach(Arrow a in b.Arrows){Assert.That(a.Kind,Is.EqualTo(ArrowKind.Normal));Assert.That(a.Length,Is.InRange(s.MinArrowLength,s.MaxArrowLength));foreach(Cell c in a.Cells)Assert.That(r.Mask.Contains(c.X,c.Y),Is.True);occupied+=a.Length;}
            Assert.That(r.CoveragePermille,Is.EqualTo(occupied*1000/r.Mask.Area).And.GreaterThanOrEqualTo(s.CoverageTargetPermille));
            int free=LevelSolvability.CountFreeArrows(b);Assert.That(free,Is.GreaterThanOrEqualTo(1));Assert.That(free*1000,Is.LessThanOrEqualTo(s.MaxFreeAtStartPermille*b.Arrows.Count));
            for(int d=0;d<4;d++)Assert.That(b.Arrows.Count(a=>(int)a.Direction==d)*100,Is.LessThanOrEqualTo(45*b.Arrows.Count));
            Assert.That(b.Arrows.Select(a=>a.Id),Is.EqualTo(Enumerable.Range(1,b.Arrows.Count)));
        }
        /// <summary>Frozen Hexagon 18x16 seed 12345 uses an explicit 500-attempt budget.</summary>
        [Test]public void GoldenSnapshot()
        {
            // If this test changes, every future daily puzzle changes. Only update it on purpose.
            var r=ShapeLevelGenerator.Generate(new ShapeLevelSettings(ShapeKind.Hexagon,18,16,maxAttempts:500),12345);
            Assert.That(r.Success,Is.True);Assert.That(Key(r.Board),Is.EqualTo("1:Left:7/10,7/11,7/12,7/13,6/13,5/13,5/14,5/15,4/15;2:Up:10/13,10/14,10/15;3:Right:11/15,12/15;4:Up:3/10,3/11,4/11,4/12,3/12,3/13,4/13,4/14;5:Right:8/15,7/15,6/15,6/14,7/14;6:Right:8/14,9/14;7:Up:11/11,11/12,11/13,11/14;8:Up:12/13,12/14;9:Up:12/10,12/11,12/12,13/12,14/12,14/13,13/13,13/14;10:Up:10/12,10/11,9/11,9/12,9/13;11:Up:3/9,2/9,2/10,2/11;12:Right:15/10,14/10,13/10,13/11,14/11;13:Up:5/4,5/5,5/6,5/7,5/8,4/8,4/9,4/10;14:Down:8/13,8/12,8/11,8/10;15:Up:10/10,10/9,11/9,11/10;16:Right:12/9,13/9,14/9,14/8,15/8,15/9,16/9;17:Up:1/10,1/9,1/8,1/7,0/7,0/8;18:Right:10/7,10/6,9/6,9/7,9/8,10/8,11/8,12/8,13/8;19:Up:17/8,17/7,16/7,16/8;20:Down:3/6,3/7,3/8,2/8,2/7;21:Up:4/4,4/5,4/6,4/7;22:Up:8/6,8/5,7/5,7/6,7/7;23:Down:9/10,9/9,8/9,8/8,8/7;24:Down:1/5,1/6,2/6,2/5,2/4;25:Down:3/5,3/4;26:Up:7/3,7/4;27:Left:12/5,12/6,12/7,13/7,13/6,13/5,13/4,12/4,11/4;28:Left:14/6,14/7,15/7,15/6,16/6,16/5,15/5,15/4,14/4;29:Left:6/9,6/8,6/7,6/6,6/5,6/4,6/3,5/3,4/3;30:Right:9/5,10/5,10/4,9/4,8/4,8/3,9/3;31:Right:9/0,10/0,10/1,10/2,10/3,11/3;32:Right:11/2,12/2,12/3,13/3;33:Down:3/3,3/2;34:Left:7/1,7/0,6/0,6/1,5/1,5/2,4/2;35:Right:6/2,7/2,8/2;36:Up:5/0,4/0,4/1;37:Right:11/0,11/1,12/1;38:Down:14/3,14/2,13/2,13/1;39:Down:9/2,9/1,8/1,8/0;40:Right:12/0,13/0"));
        }
        /// <summary>Impossible quality and length requests fail cleanly within the attempt budget.</summary>
        [Test]public void Impossible(){var r=ShapeLevelGenerator.Generate(new ShapeLevelSettings(ShapeKind.Square,8,8,minArrowLength:100,maxArrowLength:100,maxAttempts:1),7);Assert.That(r.Success,Is.False);Assert.That(r.FailureReason,Is.Not.Empty);}
        /// <summary>Invalid settings collect all errors before generation.</summary>
        [Test]public void Invalid(){var s=new ShapeLevelSettings(ShapeKind.Square,1,1,turnBias:-1);Assert.That(s.Validate().Count,Is.GreaterThanOrEqualTo(3));Assert.Throws<ArgumentException>(()=>ShapeLevelGenerator.Generate(s,0));}
    }
}


