using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Authoring constraints for released catalog paths; sandbox builders remain unrestricted.</summary>
    public static class CatalogLevelRules
    {
        /// <summary>Minimum occupied cells, including the head.</summary>
        public const int CatalogMinArrowLength=4;
        /// <summary>Maximum occupied cells, including extended tails.</summary>
        public const int CatalogMaxArrowLength=10;
        /// <summary>Returns every starter authoring violation without altering the board.</summary>
        public static IReadOnlyList<string> ValidateStarter(Board board,int number)
        {
            var errors=new List<string>();if(board==null){errors.Add("Missing starter board.");return errors.AsReadOnly();}
            int min=number==1?4:number==2?5:6,max=number==1?5:number==2?7:8;
            if(board.Arrows.Count<min||board.Arrows.Count>max)errors.Add("Starter arrow count is outside its range.");
            if(LevelSolvability.CountFreeArrows(board)<2)errors.Add("A starter needs two initially free arrows.");
            foreach(Arrow arrow in board.Arrows)
            {if(arrow.Length<4||arrow.Length>10)errors.Add("Starter path length is outside 4..10.");int turns=0;for(int i=2;i<arrow.Length;i++)if(arrow.Cells[i].X-arrow.Cells[i-1].X!=arrow.Cells[i-1].X-arrow.Cells[i-2].X||arrow.Cells[i].Y-arrow.Cells[i-1].Y!=arrow.Cells[i-1].Y-arrow.Cells[i-2].Y)turns++;if(turns>1)errors.Add("A starter path has more than one bend.");}
            if(!LevelSolvability.IsSolvable(board))errors.Add("Starter is not solvable.");return errors.AsReadOnly();
        }
    }
}
