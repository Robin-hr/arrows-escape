using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Cell-center rasterization followed only by trimming empty boundary rows and columns.</summary>
    public sealed class ShapeMask
    {
        private readonly bool[,] cells;
        /// <summary>Trimmed width.</summary>
        public int Width { get; }
        /// <summary>Trimmed height.</summary>
        public int Height { get; }
        /// <summary>Requested width before trimming.</summary>
        public int RequestedWidth { get; }
        /// <summary>Requested height before trimming.</summary>
        public int RequestedHeight { get; }
        /// <summary>Number of inside cells.</summary>
        public int Area { get; }
        private ShapeMask(bool[,] source,int minX,int minY,int maxX,int maxY)
        {
            RequestedWidth=source.GetLength(0);RequestedHeight=source.GetLength(1);
            Width=maxX-minX+1;Height=maxY-minY+1;cells=new bool[Width,Height];
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)if(cells[x,y]=source[x+minX,y+minY])Area++;
        }
        /// <summary>False outside the trimmed bounds.</summary>
        public bool Contains(int x,int y)=>x>=0&&y>=0&&x<Width&&y<Height&&cells[x,y];
        /// <summary>Minimum requested dimension for readable raster silhouettes.</summary>
        public static int MinimumSize(ShapeKind kind)=>kind>=ShapeKind.Heptagon?18:kind>=ShapeKind.Pentagon?12:kind<=ShapeKind.Semicircle?10:8;
        /// <summary>Default aspect with the shorter requested dimension at least size.</summary>
        public static Cell DefaultSize(ShapeKind kind,int size)
        {
            int w=1000,h=1000;
            switch(kind){case ShapeKind.EquilateralTriangle:h=866;break;case ShapeKind.IsoscelesTriangle:h=1400;break;case ShapeKind.Rectangle:h=1500;break;case ShapeKind.Rhombus:w=700;break;case ShapeKind.Oval:h=1300;break;case ShapeKind.Semicircle:h=500;break;}
            int unit=Math.Min(w,h);return new Cell((size*w+unit/2)/unit,(size*h+unit/2)/unit);
        }
        /// <summary>Uses integer coordinates and long cross products; does not correct the boundary.</summary>
        public static ShapeMask Build(ShapeKind kind,int width,int height)
        {
            if(kind<ShapeKind.Ellipse||kind>ShapeKind.Decagon)throw new ArgumentOutOfRangeException(nameof(kind));
            if(width<1||height<1||width>64||height>64)throw new ArgumentOutOfRangeException(nameof(width));
            var source=new bool[width,height];int minX=width,minY=height,maxX=-1,maxY=-1;
            Cell[] polygon=Polygon(kind);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                bool inside=polygon!=null?InPolygon(polygon,x,y,width,height):InCurve(kind,x,y,width,height);
                if(!inside)continue;source[x,y]=true;minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
            }
            if(maxX<0)throw new ArgumentException("Requested box contains no cell centers inside the shape.");
            return new ShapeMask(source,minX,minY,maxX,maxY);
        }
        /// <summary>Custom silhouette (x, y with y up); empty boundary rows and columns are trimmed.</summary>
        public static ShapeMask FromCells(bool[,] inside)
        {
            if(inside==null)throw new ArgumentNullException(nameof(inside));
            int width=inside.GetLength(0),height=inside.GetLength(1);
            if(width<1||height<1||width>64||height>64)throw new ArgumentOutOfRangeException(nameof(inside));
            int minX=width,minY=height,maxX=-1,maxY=-1;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(inside[x,y]){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(maxX<0)throw new ArgumentException("The silhouette has no inside cells.");
            return new ShapeMask((bool[,])inside.Clone(),minX,minY,maxX,maxY);
        }
        private static bool InCurve(ShapeKind kind,int x,int y,int w,int h)
        {
            long dx=2*x+1-w,dy=2*y+1-h,ww=(long)w*w,hh=(long)h*h;
            if(kind==ShapeKind.Semicircle){long yy=2*y+1;return 4*dx*dx*hh+yy*yy*ww<=4*ww*hh;}
            if(kind==ShapeKind.Oval&&dy>0)return 16*dx*dx*hh+9*dy*dy*ww<=9*ww*hh;
            return dx*dx*hh+dy*dy*ww<=ww*hh;
        }
        private static bool InPolygon(Cell[] p,int x,int y,int w,int h)
        {
            long px=(2L*x+1)*1000000,py=(2L*y+1)*1000000;
            for(int i=0;i<p.Length;i++)
            {
                Cell a=p[i],b=p[(i+1)%p.Length];long ax=2L*w*a.X,ay=2L*h*a.Y;
                if((2L*w*b.X-ax)*(py-ay)-(2L*h*b.Y-ay)*(px-ax)<0)return false;
            }
            return true;
        }
        /// <summary>Million-unit polygon vertices; curved shapes return an empty list.</summary>
        public static IReadOnlyList<Cell> GetPolygonVertices(ShapeKind kind)=>Array.AsReadOnly(Polygon(kind)??Array.Empty<Cell>());
        private static Cell[] Polygon(ShapeKind kind)
        {
            const int m=1000000;
            switch(kind)
            {
                case ShapeKind.Ellipse:case ShapeKind.Oval:case ShapeKind.Semicircle:return null;
                case ShapeKind.EquilateralTriangle:case ShapeKind.IsoscelesTriangle:return new[]{new Cell(0,0),new Cell(m,0),new Cell(m/2,m)};
                case ShapeKind.ScaleneTriangle:return new[]{new Cell(0,0),new Cell(m,0),new Cell(m/4,m)};
                case ShapeKind.RightTriangle:return new[]{new Cell(0,0),new Cell(m,0),new Cell(0,m)};
                case ShapeKind.Square:case ShapeKind.Rectangle:return new[]{new Cell(0,0),new Cell(m,0),new Cell(m,m),new Cell(0,m)};
                case ShapeKind.Rhombus:return new[]{new Cell(m/2,0),new Cell(m,m/2),new Cell(m/2,m),new Cell(0,m/2)};
                case ShapeKind.Parallelogram:return new[]{new Cell(0,0),new Cell(700000,0),new Cell(m,m),new Cell(300000,m)};
                case ShapeKind.Trapezium:return new[]{new Cell(0,0),new Cell(m,0),new Cell(800000,m),new Cell(200000,m)};
                case ShapeKind.Kite:return new[]{new Cell(m/2,0),new Cell(m,680000),new Cell(m/2,m),new Cell(0,680000)};
            }
            var raw=ShapePolygonTable.Get(5+(int)kind-(int)ShapeKind.Pentagon);int minX=m,minY=m,maxX=-m,maxY=-m;
            for(int i=0;i<raw.Count;i+=2){minX=Math.Min(minX,raw[i]);maxX=Math.Max(maxX,raw[i]);minY=Math.Min(minY,raw[i+1]);maxY=Math.Max(maxY,raw[i+1]);}
            var result=new Cell[raw.Count/2];
            for(int i=0;i<result.Length;i++)result[i]=new Cell((int)((long)(raw[2*i]-minX)*m/(maxX-minX)),(int)((long)(raw[2*i+1]-minY)*m/(maxY-minY)));
            return result;
        }
    }
}

