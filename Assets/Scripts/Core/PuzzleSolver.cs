using System;
using System.Collections.Generic;
using System.Text;
namespace ArrowEscape.Core
{
    /// <summary>Deterministic iterative search over compact positions/alive arrays, never Board clones.</summary>
    public static class PuzzleSolver
    {
        /// <summary>Maximum states expanded by ordinary solvability and hint searches.</summary>
        public const int DefaultNodeLimit=200000;
        private sealed class Definitions
        {
            internal readonly int[] Id;
            internal readonly Arrow[] Paths;
            internal readonly Direction[] Direction;
            internal readonly ArrowKind[] Kind;
            internal int Length=>Id.Length;
            internal Definitions(Arrow[] sorted)
            {
                Paths=sorted;Id=new int[sorted.Length];Direction=new Direction[sorted.Length];Kind=new ArrowKind[sorted.Length];
                for(int i=0;i<sorted.Length;i++){Id[i]=sorted[i].Id;Direction[i]=sorted[i].Direction;Kind[i]=sorted[i].Kind;}
            }
        }
        private sealed class Frame
        {
            internal int[] X,Y;
            internal bool[] Alive;
            internal int Cursor,PathBase;
            internal bool Entered;
            internal string Key;
            internal Frame(int[] x,int[] y,bool[] alive,int pathBase) { X=x;Y=y;Alive=alive;PathBase=pathBase; }
            internal Frame Copy(int pathBase)=>new Frame((int[])X.Clone(),(int[])Y.Clone(),(bool[])Alive.Clone(),pathBase);
        }
        /// <summary>Searches legal exits and slides, returning Unknown rather than exceeding the budget.</summary>
        public static SolveResult Analyze(Board board,int nodeLimit=DefaultNodeLimit)
        {
            if(board==null)throw new ArgumentNullException(nameof(board));
            if(nodeLimit<1)throw new ArgumentOutOfRangeException(nameof(nodeLimit));
            var arrows=new Arrow[board.Arrows.Count];for(int i=0;i<arrows.Length;i++)arrows[i]=board.Arrows[i];
            Array.Sort(arrows,(a,b)=>a.Id.CompareTo(b.Id));
            var definitions=new Definitions(arrows);
            var root=new Frame(new int[arrows.Length],new int[arrows.Length],new bool[arrows.Length],0);
            for(int i=0;i<arrows.Length;i++){root.X[i]=arrows[i].X;root.Y[i]=arrows[i].Y;root.Alive[i]=true;}
            var stack=new Stack<Frame>();stack.Push(root);var dead=new HashSet<string>();var path=new List<SolverMove>();int nodes=0;
            while(stack.Count>0)
            {
                Frame f=stack.Peek();
                if(!f.Entered)
                {
                    if(nodes==nodeLimit)return new SolveResult(SolveStatus.Unknown,nodes);
                    nodes++;f.Entered=true;Reduce(f,definitions,path);
                    if(Cleared(f))return new SolveResult(SolveStatus.Solved,nodes,path);
                    f.Key=Key(f,definitions);
                    if(dead.Contains(f.Key)){Backtrack(stack,path);continue;}
                }
                if(TryNext(f,definitions,out int index,out SolverMove move))
                {
                    Frame child=f.Copy(path.Count);path.Add(move);
                    if(move.IsSlide){child.X[index]=move.ToX;child.Y[index]=move.ToY;}else child.Alive[index]=false;
                    stack.Push(child);
                }
                else {dead.Add(f.Key);Backtrack(stack,path);}
            }
            return new SolveResult(SolveStatus.Unsolvable,nodes);
        }
        private static void Backtrack(Stack<Frame> stack,List<SolverMove> path)
        {int count=stack.Pop().PathBase;if(path.Count>count)path.RemoveRange(count,path.Count-count);}
        private static bool Cleared(Frame f){for(int i=0;i<f.Alive.Length;i++)if(f.Alive[i])return false;return true;}
        private static void Reduce(Frame f,Definitions arrows,List<SolverMove> path)
        {
            bool changed;
            do {changed=false;for(int i=0;i<arrows.Length;i++)if(f.Alive[i]&&Blocker(f,arrows,i)<0&&Safe(f,arrows,i))
                {f.Alive[i]=false;path.Add(new SolverMove(arrows.Id[i],false));changed=true;break;}}
            while(changed);
        }
        private static bool TryNext(Frame f,Definitions arrows,out int index,out SolverMove move)
        {
            while(f.Cursor<2*arrows.Length)
            {
                int cursor=f.Cursor++;index=cursor%arrows.Length;if(!f.Alive[index])continue;
                int blocker=Blocker(f,arrows,index);
                if(cursor<arrows.Length&&blocker<0){move=new SolverMove(arrows.Id[index],false);return true;}
                if(cursor>=arrows.Length&&blocker>=0&&arrows.Kind[index]==ArrowKind.Ice)
                {
                    int gap=(int)BlockerDistance(f,arrows,index)-1;
                    if(gap>0){move=new SolverMove(arrows.Id[index],true,f.X[index]+arrows.Direction[index].GetDx()*gap,f.Y[index]+arrows.Direction[index].GetDy()*gap);return true;}
                }
            }
            index=-1;move=default;return false;
        }
        private static int Blocker(Frame f,Definitions arrows,int index)
        {
            long nearest=long.MaxValue;int result=-1;
            for(int i=0;i<arrows.Length;i++)if(f.Alive[i])
                for(int c=0;c<arrows.Paths[i].Length;c++)
                {
                    Cell cell=arrows.Paths[i].Cells[c];
                    int x=arrows.Paths[i].Length==1?f.X[i]:cell.X,y=arrows.Paths[i].Length==1?f.Y[i]:cell.Y;
                    long distance=Ahead(f.X[index],f.Y[index],arrows.Direction[index],x,y);
                    if(distance>0&&distance<nearest){nearest=distance;result=i;}
                }
            return result;
        }
        private static long BlockerDistance(Frame f,Definitions arrows,int index)
        {
            int blocker=Blocker(f,arrows,index);long nearest=long.MaxValue;
            if(blocker<0)return nearest;
            for(int c=0;c<arrows.Paths[blocker].Length;c++)
            {
                Cell cell=arrows.Paths[blocker].Cells[c];
                long distance=Ahead(f.X[index],f.Y[index],arrows.Direction[index],arrows.Paths[blocker].Length==1?f.X[blocker]:cell.X,arrows.Paths[blocker].Length==1?f.Y[blocker]:cell.Y);
                if(distance>0&&distance<nearest)nearest=distance;
            }
            return nearest;
        }
        private static long Ahead(int x,int y,Direction direction,int otherX,int otherY)
        {int dx=direction.GetDx(),dy=direction.GetDy();if(dx!=0)return y==otherY?((long)otherX-x)*dx:0;return x==otherX?((long)otherY-y)*dy:0;}
        private static bool Safe(Frame f,Definitions arrows,int index)
        {
            for(int i=0;i<arrows.Length;i++)if(i!=index&&f.Alive[i]&&arrows.Kind[i]==ArrowKind.Ice)
                for(int c=0;c<arrows.Paths[index].Length;c++)
                {Cell cell=arrows.Paths[index].Cells[c];if(Ahead(f.X[i],f.Y[i],arrows.Direction[i],arrows.Paths[index].Length==1?f.X[index]:cell.X,arrows.Paths[index].Length==1?f.Y[index]:cell.Y)>0)return false;}
            return true;
        }
        /// <summary>A free arrow is safe only outside every other Ice arrow's full forward line.</summary>
        public static bool IsSafeExit(Board board,Arrow arrow)
        {
            if(board==null||!board.CanExit(arrow))return false;
            // Removing this free arrow cannot change an ice slide stop and can only free paths:
            // free arrows never move, and each advancing ice arrow's forward line only shrinks.
            foreach(Arrow other in board.Arrows)if(other!=arrow&&other.Kind==ArrowKind.Ice)
                for(int c=0;c<arrow.Length;c++)if(Ahead(other.X,other.Y,other.Direction,arrow.Cells[c].X,arrow.Cells[c].Y)>0)return false;
            return true;
        }
        private static string Key(Frame f,Definitions arrows)
        {
            var key=new StringBuilder(arrows.Length*4);
            for(int i=0;i<arrows.Length;i++){key.Append(f.Alive[i]?'1':'0');if(f.Alive[i]&&arrows.Kind[i]==ArrowKind.Ice)key.Append(':').Append(f.X[i]).Append(',').Append(f.Y[i]).Append(';');}
            return key.ToString();
        }
    }
}

