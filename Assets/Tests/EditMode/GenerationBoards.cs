using ArrowEscape.Core;
using ArrowEscape.Editor;
namespace ArrowEscape.Tests
{
    /// <summary>String snapshots and public-rule replay shared by generation tests.</summary>
    public static class GenerationBoards
    {
        /// <summary>Includes every immutable arrow property in a reading-order grid snapshot.</summary>
        public static string Snapshot(Board board)=>LevelGenerationReport.Grid(board).Replace("\r\n","\n").TrimEnd();
        /// <summary>Returns an all-Normal position copy.</summary>
        public static Board Normal(Board board)=>IceTestBoards.NormalCopy(board);
        /// <summary>Checks all filters, ids, exact counts, solvability and reversed-operation legality.</summary>
        public static void Verify(GenerationResult result,GeneratorSettings settings)
        {
            NUnit.Framework.Assert.That(result.Success,NUnit.Framework.Is.True);
            var board=result.Board;NUnit.Framework.Assert.That(board.Arrows.Count,NUnit.Framework.Is.EqualTo(settings.ArrowCount));
            int ice=0;for(int i=0;i<board.Arrows.Count;i++){var a=board.Arrows[i];if(a.Kind==ArrowKind.Ice)ice++;NUnit.Framework.Assert.That(a.Id,NUnit.Framework.Is.EqualTo(i+1));
                if(i>0){var before=board.Arrows[i-1];NUnit.Framework.Assert.That(a.Y<before.Y||a.Y==before.Y&&a.X>before.X,NUnit.Framework.Is.True);}}
            NUnit.Framework.Assert.That(ice,NUnit.Framework.Is.EqualTo(settings.IceCount));
            NUnit.Framework.Assert.That(PuzzleSolver.Analyze(board).Status,NUnit.Framework.Is.EqualTo(SolveStatus.Solved));
            int free=LevelSolvability.CountFreeArrows(board);NUnit.Framework.Assert.That(free,NUnit.Framework.Is.GreaterThanOrEqualTo(settings.MinFreeExitsAtStart));
            if(settings.ArrowCount>2)NUnit.Framework.Assert.That(free,NUnit.Framework.Is.LessThan(settings.ArrowCount));
            if(settings.ArrowCount>=8){int[] directions=new int[4];foreach(var a in board.Arrows)directions[(int)a.Direction]++;foreach(int count in directions)NUnit.Framework.Assert.That(count*100,NUnit.Framework.Is.LessThanOrEqualTo(settings.ArrowCount*60));}
            if(settings.IceCount>0&&settings.RequireSlides){NUnit.Framework.Assert.That(LevelSolvability.IsSolvable(Normal(board)),NUnit.Framework.Is.False);NUnit.Framework.Assert.That(result.Analysis.SlidesInSolution,NUnit.Framework.Is.GreaterThanOrEqualTo(1));}
            var session=new GameSession(board.Clone());int slides=0,intendedSlides=0;
            foreach(SolverMove move in result.IntendedSolution)
            {
                if(move.IsSlide)intendedSlides++;var action=session.TryMove(session.Board.GetArrowById(move.ArrowId));
                NUnit.Framework.Assert.That(action.Outcome,NUnit.Framework.Is.EqualTo(move.IsSlide?MoveOutcome.Slid:MoveOutcome.Exited));
                if(move.IsSlide){slides++;NUnit.Framework.Assert.That(action.MovedArrow.X,NUnit.Framework.Is.EqualTo(move.ToX));NUnit.Framework.Assert.That(action.MovedArrow.Y,NUnit.Framework.Is.EqualTo(move.ToY));}
            }
            NUnit.Framework.Assert.That(session.IsCleared,NUnit.Framework.Is.True);NUnit.Framework.Assert.That(session.MistakeCount,NUnit.Framework.Is.Zero);
            NUnit.Framework.Assert.That(session.MoveCount,NUnit.Framework.Is.EqualTo(settings.ArrowCount));NUnit.Framework.Assert.That(slides,NUnit.Framework.Is.EqualTo(intendedSlides));
        }
    }
}
