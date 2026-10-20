using System.Collections.Generic;
using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>Three area bands; final catalog order is determined by generated scores and variety.</summary>
    public static class ShapePackPlan
    {
        /// <summary>Closest default-aspect mask area within minimum size and phone limits.</summary>
        public static Cell SizeForArea(ShapeKind shape,int target)
        {
            Cell best=ShapeMask.DefaultSize(shape,ShapeMask.MinimumSize(shape));
            int distance=int.MaxValue;
            for(int side=ShapeMask.MinimumSize(shape);side<=36;side++)
            {
                Cell size=ShapeMask.DefaultSize(shape,side);
                if(size.X>24||size.Y>36)continue;
                int delta=System.Math.Abs(ShapeMask.Build(shape,size.X,size.Y).Area-target);
                if(delta<distance){best=size;distance=delta;}
            }
            return best;
        }
        /// <summary>Final numbered plan, derived from the builder rather than fixed shape names.</summary>
        public static IReadOnlyList<ShapePackEntry> Create()
        {
            var entries=new List<ShapePackEntry>();
            foreach(var item in ShapePackBuilder.PreparePack())entries.Add(item.Entry);
            return entries.AsReadOnly();
        }
        /// <summary>Creates all 57 immutable entries without generating any boards.</summary>
        public static IReadOnlyList<ShapePackEntry> CreateRequests()
        {
            var order=new[]{ShapeKind.Square,ShapeKind.Rectangle,ShapeKind.RightTriangle,ShapeKind.IsoscelesTriangle,ShapeKind.EquilateralTriangle,ShapeKind.ScaleneTriangle,ShapeKind.Rhombus,ShapeKind.Parallelogram,ShapeKind.Trapezium,ShapeKind.Kite,ShapeKind.Semicircle,ShapeKind.Ellipse,ShapeKind.Oval,ShapeKind.Pentagon,ShapeKind.Hexagon,ShapeKind.Heptagon,ShapeKind.Octagon,ShapeKind.Nonagon,ShapeKind.Decagon};
            var result=new List<ShapePackEntry>();
            int[] bands={70,180,330};
            // Reviewed deterministic seeds; no tier is requested from the generator.
            int[] seeds={723,11,56,4,26,3,140,25,5,19,24,5,65,39,5,0,5,3,23,
                1007,1039,1018,1000,1013,1003,1054,1008,1009,1038,1002,1006,1009,1001,1005,1000,1017,1002,1004,
                2005,2039,2039,2064,2040,2056,2057,2004,2003,2028,2042,2003,2005,2008,2011,2015,2034,2004,2004};
            for(int round=0;round<3;round++)foreach(ShapeKind shape in order)
            {
                Cell size=SizeForArea(shape,bands[round]);
                result.Add(new ShapePackEntry(result.Count+4,shape,size.X,size.Y,seeds[result.Count],targetArea:bands[round]));
            }
            return result.AsReadOnly();
        }
    }
}



