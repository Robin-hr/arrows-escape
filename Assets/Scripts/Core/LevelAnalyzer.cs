using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Integer-only, bounded exhaustive analysis and deterministic random playouts.</summary>
    public static class LevelAnalyzer
    {
        /// <summary>Clearing-sequence saturation limit.</summary>
        public const long SolutionLimit=1000000000000000L;
        private sealed class Node
        {
            internal readonly AnalysisState State;
            internal readonly List<SolverMove> Moves;
            internal readonly List<Node> Children=new List<Node>();
            internal int Cursor;
            internal long Ways;
            internal bool Saturated;
            internal Node(AnalysisState state){State=state;Moves=state.Moves();}
        }
        /// <summary>Never mutates the board; exhaustive traversal may stop, but all playouts still run.</summary>
        public static LevelAnalysis Analyze(Board board,AnalysisOptions options=null)
        {
            if(board==null)throw new ArgumentNullException(nameof(board));options=options??new AnalysisOptions();
            SolveResult solver=PuzzleSolver.Analyze(board);
            int ice=0,free=0,slides=0,chain=0;
            foreach(Arrow arrow in board.Arrows)
            {
                if(arrow.Kind==ArrowKind.Ice)ice++;if(board.CanExit(arrow))free++;if(board.CanSlide(arrow))slides++;
                var seen=new HashSet<int>();Arrow current=arrow;
                while(current!=null&&seen.Add(current.Id))current=board.GetBlocker(current);
                chain=Math.Max(chain,seen.Count);
            }
            var root=new Node(new AnalysisState(board));var nodes=new List<Node>{root};
            bool complete=Explore(root,nodes,options.MaxStates);
            int dead=0,traps=0,startTraps=0;
            if(complete)
            {
                foreach(Node node in nodes)
                {
                    if(node.State.Count>0&&node.Ways==0)dead++;
                    if(node.Ways==0)continue;
                    foreach(Node child in node.Children)if(child.Ways==0){traps++;if(node==root)startTraps++;}
                }
            }
            Playout(root.State,options,out int trapRate,out int choice);
            // SOLVABLE normal-only boards have no dead states or trap moves: exits cannot block paths.
            // Unsolvable normal boards still report their actual dead states and failed playouts.
            return new LevelAnalysis(board.Width,board.Height,board.Arrows.Count,ice,solver,free,slides,chain,
                complete,nodes.Count,dead,root.Ways,root.Saturated,traps,root.Moves.Count,startTraps,trapRate,choice);
        }
        private static bool Explore(Node root,List<Node> nodes,int limit)
        {
            var memo=new Dictionary<string,Node>{{root.State.Key(),root}};var stack=new Stack<Node>();stack.Push(root);
            // The graph is a DAG: exits reduce count; slides only advance Ice coordinates.
            while(stack.Count>0)
            {
                Node node=stack.Peek();
                if(node.Cursor<node.Moves.Count)
                {
                    AnalysisState childState=node.State.Copy();childState.Apply(node.Moves[node.Cursor++]);string key=childState.Key();
                    if(memo.TryGetValue(key,out Node known)){node.Children.Add(known);continue;}
                    if(nodes.Count==limit)return false;
                    var child=new Node(childState);memo.Add(key,child);nodes.Add(child);node.Children.Add(child);stack.Push(child);
                }
                else
                {
                    if(node.State.Count==0)node.Ways=1;
                    foreach(Node child in node.Children)
                    {
                        if(child.Saturated||node.Ways>=SolutionLimit-child.Ways){node.Ways=SolutionLimit;node.Saturated=true;}
                        else node.Ways+=child.Ways;
                    }
                    stack.Pop();
                }
            }
            return true;
        }
        private static void Playout(AnalysisState start,AnalysisOptions options,out int trapRate,out int average)
        {
            var random=new SeededRandom(options.PlayoutSeed);int failures=0;long sum=0,steps=0;
            for(int game=0;game<options.Playouts;game++)
            {
                AnalysisState state=start.Copy();
                while(state.Count>0)
                {
                    List<SolverMove> moves=state.Moves();if(moves.Count==0){failures++;break;}
                    sum+=(long)moves.Count*1000/state.Count;steps++;
                    state.Apply(moves[random.NextInt(moves.Count)]);
                }
            }
            trapRate=(int)((long)failures*1000/options.Playouts);average=steps==0?0:(int)(sum/steps);
        }
    }
}
