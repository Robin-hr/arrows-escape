using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Shared path fixtures and order-independent comparison.</summary>
    public static class PathTestBoards
    {
        /// <summary>Loads a sandbox fixture.</summary>
        public static Board Load(int number=1)
        {var result=LevelRepository.LoadFromResources("TestLevels/path_00"+number);Assert.That(result.Success,Is.True,string.Join(";",result.Errors));return result.Board;}
        /// <summary>Compares every rule-bearing field, sorted by stable id.</summary>
        public static string Signature(Board board)=>string.Join("|",board.Arrows.OrderBy(a=>a.Id).Select(a=>a.Id+":"+a.Direction+":"+a.Kind+":"+string.Join(";",a.Cells.Select(c=>c.X+","+c.Y))));
    }
}
