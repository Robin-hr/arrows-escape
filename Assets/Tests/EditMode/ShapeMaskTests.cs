using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Raster boundaries are tested without altering a single mask cell.</summary>
    public class ShapeMaskTests
    {
        /// <summary>Every supported shape remains connected after trimming.</summary>
        [Test]public void ConnectivityAndTrim()
        {
            foreach(ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))foreach(int size in new[]{ShapeMask.MinimumSize(kind),24})
            {
                Cell box=ShapeMask.DefaultSize(kind,size);var m=ShapeMask.Build(kind,box.X,box.Y);var seen=new HashSet<Cell>();var queue=new Queue<Cell>();
                for(int y=0;y<m.Height;y++)for(int x=0;x<m.Width;x++)if(m.Contains(x,y)&&seen.Count==0){var c=new Cell(x,y);seen.Add(c);queue.Enqueue(c);}
                while(queue.Count>0){Cell c=queue.Dequeue();for(int d=0;d<4;d++){var n=new Cell(c.X+((Direction)d).GetDx(),c.Y+((Direction)d).GetDy());if(m.Contains(n.X,n.Y)&&seen.Add(n))queue.Enqueue(n);}}
                Assert.That(seen.Count,Is.EqualTo(m.Area),kind.ToString());bool l=false,r=false,t=false,b=false;
                foreach(var c in seen){l|=c.X==0;r|=c.X==m.Width-1;b|=c.Y==0;t|=c.Y==m.Height-1;}Assert.That(l&&r&&t&&b,Is.True,kind.ToString());
                var again=ShapeMask.Build(kind,box.X,box.Y);foreach(var c in seen)Assert.That(again.Contains(c.X,c.Y),Is.True);
            }
        }
        /// <summary>Symmetric analytic shapes retain their mirror at cell-center resolution.</summary>
        [Test]public void MirrorSymmetry()
        {
            foreach(ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
            {
                if(kind==ShapeKind.ScaleneTriangle||kind==ShapeKind.RightTriangle||kind==ShapeKind.Parallelogram)continue;
                foreach(int n in new[]{ShapeMask.MinimumSize(kind),24}){var box=ShapeMask.DefaultSize(kind,n);var m=ShapeMask.Build(kind,box.X,box.Y);for(int y=0;y<m.Height;y++)for(int x=0;x<m.Width;x++)Assert.That(m.Contains(x,y),Is.EqualTo(m.Contains(m.Width-1-x,y)),kind.ToString());}
            }
        }
        /// <summary>The offline constants match regular vertices to one millionth.</summary>
        [Test]public void PolygonTable()
        {for(int n=5;n<=10;n++){var p=ShapePolygonTable.Get(n);for(int i=0;i<n;i++){double a=-Math.PI/2-Math.PI/n+2*Math.PI*i/n;Assert.That(p[2*i]/1000000d,Is.EqualTo(Math.Cos(a)).Within(1e-6));Assert.That(p[2*i+1]/1000000d,Is.EqualTo(Math.Sin(a)).Within(1e-6));}}}
        /// <summary>Scalene vertices have three unequal squared side lengths.</summary>
        [Test]public void ScaleneSides(){var p=ShapeMask.GetPolygonVertices(ShapeKind.ScaleneTriangle);var lengths=new HashSet<long>();for(int i=0;i<3;i++){long dx=p[i].X-p[(i+1)%3].X,dy=p[i].Y-p[(i+1)%3].Y;lengths.Add(dx*dx+dy*dy);}Assert.That(lengths.Count,Is.EqualTo(3));}
        /// <summary>Cell-center area approaches the exact requested-box area.</summary>
        [Test]public void Area()
        {
            foreach(ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
            {
                var box=ShapeMask.DefaultSize(kind,24);var mask=ShapeMask.Build(kind,box.X,box.Y);double fraction;
                if(kind>=ShapeKind.Pentagon){var p=ShapePolygonTable.Get(5+(int)kind-(int)ShapeKind.Pentagon);double area=0;int minX=int.MaxValue,minY=int.MaxValue,maxX=int.MinValue,maxY=int.MinValue;for(int i=0;i<p.Count;i+=2){int j=(i+2)%p.Count;area+=(double)p[i]*p[j+1]-(double)p[j]*p[i+1];minX=Math.Min(minX,p[i]);maxX=Math.Max(maxX,p[i]);minY=Math.Min(minY,p[i+1]);maxY=Math.Max(maxY,p[i+1]);}fraction=area/2/((double)(maxX-minX)*(maxY-minY));}
                else switch(kind){case ShapeKind.Ellipse:case ShapeKind.Semicircle:fraction=Math.PI/4;break;case ShapeKind.Oval:fraction=Math.PI/4*0.875;break;case ShapeKind.Square:case ShapeKind.Rectangle:fraction=1;break;case ShapeKind.Parallelogram:fraction=.7;break;case ShapeKind.Trapezium:fraction=.8;break;default:fraction=.5;break;}
                double exact=box.X*box.Y*fraction;Assert.That(Math.Abs(mask.Area-exact)/exact,Is.LessThanOrEqualTo(.06),kind.ToString());
            }
        }
    }
}

