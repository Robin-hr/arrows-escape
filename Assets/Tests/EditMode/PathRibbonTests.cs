using System;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Fixed-buffer geometry and moving track windows.</summary>
    public class PathRibbonTests
    {
        [Test] public void BuffersAndWindow()
        {
            var r=new PathRibbon(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(1,12)},2);
            var vertices=r.Vertices;var indices=r.Triangles;r.Build(0);Assert.That(r.Point(0),Is.EqualTo(Vector2.zero));r.Build(10);Assert.That(r.Vertices,Is.SameAs(vertices));Assert.That(r.Triangles,Is.SameAs(indices));
            foreach(var v in vertices)Assert.That(v.y,Is.GreaterThan(4));
        }
        [Test] public void RibbonStaysWithinRadiusExceptHead()
        {
            var r=new PathRibbon(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(1,8)},2);r.Build(0);
            for(int i=0;i<r.Vertices.Length-3;i++){Vector3 v=r.Vertices[i];float d1=Vector2.Distance(v,new Vector2(Mathf.Clamp(v.x,0,1),0)),d2=Vector2.Distance(v,new Vector2(1,Mathf.Clamp(v.y,0,1)));Assert.That(Mathf.Min(d1,d2),Is.LessThanOrEqualTo(.13001f));}
        }
        [Test] public void NoManagedAllocationInBuild()
        {var r=new PathRibbon(new[]{Vector2.zero,Vector2.up,Vector2.up*20},1);r.Build(0);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)r.Build(i*.01f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);}
    }
}
