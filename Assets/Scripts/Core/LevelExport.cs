using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Converts boards to the existing level DTO without JSON or file dependencies.</summary>
    public static class LevelExport
    {
        private static int[] ExportCells(Arrow arrow)
        {
            if(arrow.Length==1)return null;
            var cells=new int[arrow.Length*2];
            for(int i=0;i<arrow.Length;i++){cells[2*i]=arrow.Cells[i].X;cells[2*i+1]=arrow.Cells[i].Y;}
            return cells;
        }
        /// <summary>Exports explicit normal/ice types and direction names, ordered by id.</summary>
        public static LevelData ToLevelData(Board board,string name)
        {
            if(board==null)throw new ArgumentNullException(nameof(board));
            if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("A level name is required.",nameof(name));
            var result=new LevelData{name=name,width=board.Width,height=board.Height,arrows=new List<ArrowData>()};
            var arrows=new List<Arrow>(board.Arrows);arrows.Sort((a,b)=>a.Id.CompareTo(b.Id));
            foreach(Arrow arrow in arrows)result.arrows.Add(new ArrowData{id=arrow.Id,x=arrow.X,y=arrow.Y,direction=arrow.Direction.ToString(),type=arrow.Kind==ArrowKind.Ice?"ice":"normal",cells=ExportCells(arrow)});
            return result;
        }
    }
}
