using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Rounded million-unit unit-circle vertices, counterclockwise with a flat bottom.</summary>
    public static class ShapePolygonTable
    {
        private static readonly int[][] vertices = {
            new int[]{-587785,-809017,587785,-809017,951057,309017,0,1000000,-951057,309017},
            new int[]{-500000,-866025,500000,-866025,1000000,0,500000,866025,-500000,866025,-1000000,0},
            new int[]{-433884,-900969,433884,-900969,974928,-222521,781831,623490,0,1000000,-781831,623490,-974928,-222521},
            new int[]{-382683,-923880,382683,-923880,923880,-382683,923880,382683,382683,923880,-382683,923880,-923880,382683,-923880,-382683},
            new int[]{-342020,-939693,342020,-939693,866025,-500000,984808,173648,642788,766044,0,1000000,-642788,766044,-984808,173648,-866025,-500000},
            new int[]{-309017,-951057,309017,-951057,809017,-587785,1000000,0,809017,587785,309017,951057,-309017,951057,-809017,587785,-1000000,0,-809017,-587785} };
        /// <summary>Returns an immutable view of raw x/y unit-circle pairs for 5..10 sides.</summary>
        public static IReadOnlyList<int> Get(int sides)
        {
            if(sides<5||sides>10)throw new ArgumentOutOfRangeException(nameof(sides));
            return Array.AsReadOnly(vertices[sides-5]);
        }
    }
}
