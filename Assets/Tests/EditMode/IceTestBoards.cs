using System.Linq;
using ArrowEscape.Core;
namespace ArrowEscape.Tests
{
    /// <summary>Ground-truth boards and seeded small-board samples shared by ice tests.</summary>
    public static class IceTestBoards
    {
        /// <summary>The four-arrow blocked cycle whose first useful move must slide.</summary>
        public static Board Cycle()
        {var b=new Board(4,3);b.TryAddArrow(1,0,0,Direction.Right,ArrowKind.Ice,out _);b.TryAddArrow(2,3,0,Direction.Up,out _);b.TryAddArrow(3,3,2,Direction.Left,out _);b.TryAddArrow(4,0,2,Direction.Down,out _);return b;}
        /// <summary>The solvable board with the exit-2/slide-1 dead-end trap.</summary>
        public static Board Trap()
        {var b=new Board(6,3);b.TryAddArrow(1,0,0,Direction.Right,ArrowKind.Ice,out _);b.TryAddArrow(2,3,0,Direction.Up,out _);b.TryAddArrow(3,4,2,Direction.Down,out _);b.TryAddArrow(4,5,2,Direction.Left,out _);b.TryAddArrow(5,5,0,Direction.Up,out _);return b;}
        /// <summary>Copies positions and directions but replaces every kind with Normal.</summary>
        public static Board NormalCopy(Board source){var b=new Board(source.Width,source.Height);foreach(var a in source.Arrows)b.TryAddArrow(a.Id,a.X,a.Y,a.Direction,out _);return b;}
        /// <summary>Canonical board state including identity, direction and kind.</summary>
        public static string Key(Board b)=>string.Join(";",b.Arrows.OrderBy(a=>a.Id).Select(a=>$"{a.Id},{a.X},{a.Y},{(int)a.Direction},{(int)a.Kind}"));
        /// <summary>Applies a legal solver instruction using only Board's public operations.</summary>
        public static bool Apply(Board b,SolverMove move)
        {
            Arrow a=b.GetArrowById(move.ArrowId);if(a==null)return false;
            if(!move.IsSlide)return b.CanExit(a)&&b.Remove(a);
            if(a.Kind!=ArrowKind.Ice||b.CanExit(a))return false;int gap=b.CountFreeCellsAhead(a);
            return gap>0&&move.ToX==a.X+a.Direction.GetDx()*gap&&move.ToY==a.Y+a.Direction.GetDy()*gap&&b.TryMoveArrow(a,move.ToX,move.ToY,out _);
        }
        /// <summary>Seeded 4x4/5x3 samples include ice cycles and adjacent opposing normal pairs.</summary>
        public static Board RandomBoard(int seed)
        {
            var r=new DeterministicRandom(seed+1973);int width=seed%2==0?4:5,height=seed%2==0?4:3;
            var b=new Board(width,height);int count=3+(int)(r.NextFloat()*6),ice=(int)(r.NextFloat()*4),next=1;
            if(seed%15==0)
            {
                count=System.Math.Max(4,count);ice=System.Math.Max(1,ice);int iceCorner=(int)(r.NextFloat()*4);bool mirror=r.NextFloat()<.5f;
                int[] xs={0,width-1,width-1,0},ys={0,0,height-1,height-1};Direction[] ds={Direction.Right,Direction.Up,Direction.Left,Direction.Down};
                for(int i=0;i<4;i++){int x=mirror?width-1-xs[i]:xs[i];Direction d=ds[i];if(mirror&&(d==Direction.Left||d==Direction.Right))d=d.Opposite();b.TryAddArrow(next++,x,ys[i],d,i==iceCorner?ArrowKind.Ice:ArrowKind.Normal,out _);}ice--;
            }
            if(seed%10==1){b.TryAddArrow(next++,0,0,Direction.Right,out _);b.TryAddArrow(next++,1,0,Direction.Left,out _);}
            while(b.Arrows.Count<count)
            {int x=(int)(r.NextFloat()*width),y=(int)(r.NextFloat()*height);if(b.GetArrowAt(x,y)!=null)continue;Direction d=(Direction)(int)(r.NextFloat()*4);ArrowKind kind=ice>0?ArrowKind.Ice:ArrowKind.Normal;b.TryAddArrow(next++,x,y,d,kind,out _);if(ice>0)ice--;}
            return b;
        }
    }
}

