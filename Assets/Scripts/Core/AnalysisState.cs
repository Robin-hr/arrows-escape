using System;
using System.Collections.Generic;
using System.Text;
namespace ArrowEscape.Core
{
    // Analyzer-owned compact state. Definitions are immutable and shared; only coordinates/alive copy.
    internal sealed class AnalysisState
    {
        internal readonly Arrow[] Definitions;
        internal readonly int[] X,Y;
        internal readonly bool[] Alive;
        private readonly int[][] normalBlockers;
        internal int Count { get; private set; }
        internal AnalysisState(Board board)
        {
            Definitions=new Arrow[board.Arrows.Count];
            for(int i=0;i<Definitions.Length;i++)Definitions[i]=board.Arrows[i];
            Array.Sort(Definitions,(a,b)=>a.Id.CompareTo(b.Id));
            X=new int[Definitions.Length];Y=new int[Definitions.Length];Alive=new bool[Definitions.Length];
            for(int i=0;i<Definitions.Length;i++){X[i]=Definitions[i].X;Y[i]=Definitions[i].Y;Alive[i]=true;}Count=Alive.Length;
            bool hasIce=false;foreach(Arrow arrow in Definitions)if(arrow.Kind==ArrowKind.Ice)hasIce=true;
            if(!hasIce)normalBlockers=BuildNormalBlockers();
        }
        private AnalysisState(AnalysisState other)
        {Definitions=other.Definitions;normalBlockers=other.normalBlockers;X=(int[])other.X.Clone();Y=(int[])other.Y.Clone();Alive=(bool[])other.Alive.Clone();Count=other.Count;}
        internal AnalysisState Copy()=>new AnalysisState(this);
        internal List<SolverMove> Moves()
        {
            var exits=new List<SolverMove>();var slides=new List<SolverMove>();
            for(int i=0;i<Alive.Length;i++)
            {
                if(!Alive[i])continue;int blocker=Blocker(i);
                if(blocker<0)exits.Add(new SolverMove(Definitions[i].Id,false));
                else if(Definitions[i].Kind==ArrowKind.Ice)
                {
                    int dx=Definitions[i].Direction.GetDx(),dy=Definitions[i].Direction.GetDy();
                    int gap=(int)Distance(i)-1;
                    if(gap>0)slides.Add(new SolverMove(Definitions[i].Id,true,X[i]+dx*gap,Y[i]+dy*gap));
                }
            }
            exits.AddRange(slides);return exits;
        }
        internal void Apply(SolverMove move)
        {
            int index=Array.FindIndex(Definitions,a=>a.Id==move.ArrowId);
            if(move.IsSlide){X[index]=move.ToX;Y[index]=move.ToY;}
            else {Alive[index]=false;Count--;}
        }
        private int Blocker(int index)
        {
            if(normalBlockers!=null){foreach(int other in normalBlockers[index])if(Alive[other])return other;return -1;}
            int nearest=-1;long distance=long.MaxValue;
            for(int i=0;i<Alive.Length;i++)if(Alive[i])
                for(int c=0;c<Definitions[i].Length;c++)
                {long ahead=Ahead(index,i,c);if(ahead>0&&ahead<distance){distance=ahead;nearest=i;}}
            return nearest;
        }
        // Normal paths never relocate. Cache their exact nearest-first blocking owners once;
        // descendants only change Alive flags. Ice states retain the original coordinate search.
        private int[][] BuildNormalBlockers()
        {
            var result=new int[Definitions.Length][];
            for(int index=0;index<Definitions.Length;index++)
            {
                var owners=new List<int>();var distances=new long[Definitions.Length];
                for(int other=0;other<Definitions.Length;other++)
                {
                    long nearest=long.MaxValue;
                    for(int cell=0;cell<Definitions[other].Length;cell++)
                    {long ahead=Ahead(index,other,cell);if(ahead>0&&ahead<nearest)nearest=ahead;}
                    distances[other]=nearest;if(nearest!=long.MaxValue)owners.Add(other);
                }
                owners.Sort((a,b)=>distances[a]!=distances[b]?distances[a].CompareTo(distances[b]):a.CompareTo(b));result[index]=owners.ToArray();
            }
            return result;
        }
        private long Distance(int index)
        {
            long distance=long.MaxValue;int blocker=Blocker(index);
            if(blocker>=0)for(int c=0;c<Definitions[blocker].Length;c++)
            {long ahead=Ahead(index,blocker,c);if(ahead>0&&ahead<distance)distance=ahead;}
            return distance;
        }
        private long Ahead(int index,int other,int c)
        {
            Cell cell=Definitions[other].Cells[c];int x=Definitions[other].Length==1?X[other]:cell.X,y=Definitions[other].Length==1?Y[other]:cell.Y;
            int dx=Definitions[index].Direction.GetDx(),dy=Definitions[index].Direction.GetDy();
            return dx!=0?(y==Y[index]?((long)x-X[index])*dx:0):(x==X[index]?((long)y-Y[index])*dy:0);
        }
        internal string Key()
        {
            var text=new StringBuilder(Alive.Length*4);
            for(int i=0;i<Alive.Length;i++)
            {
                text.Append(Alive[i]?'1':'0');
                if(Alive[i]&&Definitions[i].Kind==ArrowKind.Ice)text.Append(':').Append(X[i]).Append(',').Append(Y[i]).Append(';');
            }
            return text.ToString();
        }
    }
}
