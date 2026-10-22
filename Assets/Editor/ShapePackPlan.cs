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
            return CatalogShapePlan.Create();
        }
    }
}



