using System.Text;
using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>Cached Editor display and measurement-report formatting.</summary>
    public static class LevelGenerationReport
    {
        /// <summary>Prints top row first; row zero is the bottom line, and Ice uses an asterisk.</summary>
        public static string Grid(Board board)
        {
            var text=new StringBuilder();
            for(int y=board.Height-1;y>=0;y--)
            {
                text.Append("y").Append(y).Append("  ");
                for(int x=0;x<board.Width;x++)
                {
                    Arrow arrow=board.GetArrowAt(x,y);char symbol=arrow==null?'.':arrow.Direction==Direction.Up?'^':arrow.Direction==Direction.Down?'v':arrow.Direction==Direction.Left?'<':'>';
                    text.Append(arrow!=null&&arrow.Kind==ArrowKind.Ice?'*':' ').Append(symbol).Append(' ');
                }
                text.AppendLine();
            }
            return text.ToString();
        }
        /// <summary>Formats every analysis number and normalized score component.</summary>
        public static string Analysis(LevelAnalysis a,DifficultyRating r)
        {
            return $"Size {a.Width}x{a.Height}; arrows {a.ArrowCount}; ice {a.IceCount}\n"+
                $"Status {a.Status}; solver slides {a.SlidesInSolution}; free exits {a.FreeExitsAtStart}; start slides {a.SlidesAtStart}; max chain {a.MaxBlockerChain}\n"+
                $"Exhaustive complete {a.ExhaustiveComplete}; reachable {a.ReachableStates}; dead {a.DeadStates}; solutions {a.SolutionCount}; saturated {a.SolutionCountSaturated}\n"+
                $"Trap moves {a.TrapMoves}; start moves {a.StartMoves}; start traps {a.StartTrapMoves}; trap rate {a.TrapRatePermille}; avg choice {a.AvgChoicePermille}\n"+
                $"Score {r.Score}; tier {r.Tier}; components: size {r.Size}, scarcity {r.Scarcity}, chain {r.Chain}, traps {r.Traps}, slides {r.Slides}";
        }
        /// <summary>Formats a complete replayable move list.</summary>
        public static string Moves(System.Collections.Generic.IReadOnlyList<SolverMove> path)
        {
            if(path==null)return "none";var text=new StringBuilder();
            for(int i=0;i<path.Count;i++){if(i>0)text.Append(", ");var move=path[i];text.Append(move.ArrowId);if(move.IsSlide)text.Append(" slide(").Append(move.ToX).Append(',').Append(move.ToY).Append(')');else text.Append(" exit");}
            return text.ToString();
        }
    }
}
