using System.Collections.Generic;
using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>Reviewed long-path seeds retain the released shape identities and score order.</summary>
    public static class CatalogShapePlan
    {
        /// <summary>Exactly 57 deterministic catalog replacements, without a tier override.</summary>
        public static IReadOnlyList<ShapePackEntry> Create()
        {
            return new ShapePackEntry[]
            {
                new ShapePackEntry(4,ShapeKind.Square,8,8,43086,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(5,ShapeKind.IsoscelesTriangle,10,14,52031,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(6,ShapeKind.RightTriangle,11,11,61397,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(7,ShapeKind.Kite,12,12,72092,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(8,ShapeKind.ScaleneTriangle,12,12,80413,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(9,ShapeKind.Semicircle,20,10,90705,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(10,ShapeKind.EquilateralTriangle,13,11,103382,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(11,ShapeKind.Hexagon,12,12,110323,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(12,ShapeKind.Pentagon,12,12,121755,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(13,ShapeKind.Parallelogram,10,10,132388,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(14,ShapeKind.Ellipse,10,10,143875,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(15,ShapeKind.RightTriangle,18,18,152370,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(16,ShapeKind.Trapezium,9,9,163622,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(17,ShapeKind.Rhombus,10,14,171508,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(18,ShapeKind.Rectangle,8,12,183762,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(19,ShapeKind.Semicircle,22,11,190982,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(20,ShapeKind.IsoscelesTriangle,16,22,203272,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(21,ShapeKind.Parallelogram,16,16,212028,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(22,ShapeKind.EquilateralTriangle,20,17,220493,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(23,ShapeKind.Oval,10,13,231356,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(24,ShapeKind.Rhombus,16,23,243891,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(25,ShapeKind.Ellipse,15,15,250654,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(26,ShapeKind.Nonagon,18,18,260887,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(27,ShapeKind.Pentagon,16,16,272034,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(28,ShapeKind.Oval,14,18,283960,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(29,ShapeKind.Trapezium,15,15,291179,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(30,ShapeKind.Kite,24,24,5000362,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(31,ShapeKind.Nonagon,18,18,310836,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(32,ShapeKind.EquilateralTriangle,24,21,4019788,targetArea:330,turnBias:0,reviewed:true),
                new ShapePackEntry(33,ShapeKind.Heptagon,18,18,331267,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(34,ShapeKind.RightTriangle,24,24,3005457,targetArea:330,turnBias:20,reviewed:true),
                new ShapePackEntry(35,ShapeKind.Semicircle,24,12,2013971,targetArea:330,turnBias:70,reviewed:true),
                new ShapePackEntry(36,ShapeKind.ScaleneTriangle,19,19,363054,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(37,ShapeKind.Hexagon,15,15,370722,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(38,ShapeKind.Decagon,18,18,383974,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(39,ShapeKind.Heptagon,18,18,392877,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(40,ShapeKind.Rectangle,11,17,402724,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(41,ShapeKind.Octagon,18,18,410963,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(42,ShapeKind.Square,13,13,420909,targetArea:180,turnBias:45,reviewed:true),
                new ShapePackEntry(43,ShapeKind.Parallelogram,22,22,5003609,targetArea:330,turnBias:20,reviewed:true),
                new ShapePackEntry(44,ShapeKind.Trapezium,20,20,443865,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(45,ShapeKind.ScaleneTriangle,24,24,452750,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(46,ShapeKind.Rhombus,22,31,462332,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(47,ShapeKind.Pentagon,22,22,471563,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(48,ShapeKind.Square,18,18,5015936,targetArea:330,turnBias:0,reviewed:true),
                new ShapePackEntry(49,ShapeKind.Oval,19,25,6002021,targetArea:330,turnBias:20,reviewed:true),
                new ShapePackEntry(50,ShapeKind.Octagon,20,20,502695,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(51,ShapeKind.Decagon,18,18,510545,targetArea:70,turnBias:45,reviewed:true),
                new ShapePackEntry(52,ShapeKind.Kite,19,19,5019241,targetArea:180,turnBias:20,reviewed:true),
                new ShapePackEntry(53,ShapeKind.Ellipse,20,20,531983,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(54,ShapeKind.Nonagon,21,21,543956,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(55,ShapeKind.Octagon,18,18,5000357,targetArea:70,turnBias:20,reviewed:true),
                new ShapePackEntry(56,ShapeKind.Rectangle,15,23,562921,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(57,ShapeKind.Heptagon,21,21,570094,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(58,ShapeKind.Decagon,21,21,582241,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(59,ShapeKind.IsoscelesTriangle,22,31,593604,targetArea:330,turnBias:45,reviewed:true),
                new ShapePackEntry(60,ShapeKind.Hexagon,21,21,603192,targetArea:330,turnBias:45,reviewed:true),
            };
        }
    }
}