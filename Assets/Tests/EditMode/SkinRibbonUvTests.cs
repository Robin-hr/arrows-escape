using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Cell-distance UVs share buffers and never stretch a repeated pattern.</summary>
    public sealed class SkinRibbonUvTests
    {
        /// <summary>U is geometric cell distance at both straight ribbon ends.</summary>
        [TestCase(2)][TestCase(8)][TestCase(20)] public void DistanceNotNormalized(int length)
        {
            var ribbon=new PathRibbon(new[]{Vector2.zero,new Vector2(length,0),new Vector2(length+10,0)},length);ribbon.Build(0);Assert.That(ribbon.UV[0].x,Is.Zero);Assert.That(ribbon.UV[ribbon.UV.Length-3].x,Is.EqualTo(length));Assert.That(ribbon.UV[1].x,Is.EqualTo(length-ViewTheme.PathHeadLength).Within(.0001));
            var uv=ribbon.UV;var edge=ribbon.Edges;ribbon.Build(1);Assert.That(ribbon.UV,Is.SameAs(uv));Assert.That(ribbon.Edges,Is.SameAs(edge));Assert.That(ribbon.UV[ribbon.UV.Length-3].x,Is.EqualTo(length));
            foreach(var point in ribbon.UV)Assert.That(point.y,Is.InRange(0,1));
        }
        /// <summary>Turning geometry uses cumulative path length rather than bounding-box length.</summary>
        [Test] public void BentTrackKeepsCellScale()
        {var ribbon=new PathRibbon(new[]{Vector2.zero,new Vector2(2,0),new Vector2(2,3),new Vector2(7,3)},7);ribbon.Build(0);Assert.That(ribbon.UV[4].x,Is.EqualTo(2));Assert.That(ribbon.UV[8].x,Is.EqualTo(5));}
    }
}
