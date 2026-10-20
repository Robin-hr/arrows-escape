using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Prepares a reviewable pack; writing is a separate, explicitly confirmed operation.</summary>
    public static class ShapePackBuilder
    {
        /// <summary>First passing seed, without a requested tier or tier fallback.</summary>
        public static ShapePackItem Prepare(ShapePackEntry entry)
        {
            var watch=Stopwatch.StartNew();
            for(int offset=0;offset<50;offset++)
            {
                var settings=new ShapeLevelSettings(entry.Shape,entry.Width,entry.Height);
                var result=ShapeLevelGenerator.Generate(settings,checked(entry.BaseSeed+offset));
                if(result.Success)return new ShapePackItem(entry,result,watch.ElapsedMilliseconds);
            }
            return new ShapePackItem(entry,null,watch.ElapsedMilliseconds);
        }
        /// <summary>Builds and orders the complete plan in memory; never writes files.</summary>
        public static IReadOnlyList<ShapePackItem> PreparePack()
        {
            var items=new List<ShapePackItem>();
            foreach(var entry in ShapePackPlan.CreateRequests())items.Add(Prepare(entry));
            return ShapePackOrdering.Order(items);
        }
        /// <summary>Exact ordered resource paths produced by the current pack.</summary>
        public static IReadOnlyList<string> OrderedResourcePaths()
        {
            var paths=new List<string>();foreach(var item in PreparePack())paths.Add("Levels/"+item.Entry.Id);
            return paths.AsReadOnly();
        }
        /// <summary>Only approved shape filenames with catalog numbers four and above are accepted.</summary>
        public static bool IsPackFileName(string name)
        {
            if(name==null)return false;var match=Regex.Match(name,@"^level_([0-9]{3})_shape_([a-z]+)\.json$");
            if(!match.Success||int.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)<4)return false;
            foreach(ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))if(kind.ToString().ToLowerInvariant()==match.Groups[2].Value)return true;
            return false;
        }
        /// <summary>Writes only new approved pack names; refuses existing targets before writing any file.</summary>
        public static void WriteNew(string folder,IReadOnlyList<ShapePackItem> items)
        {
            if(items==null||items.Count==0)throw new ArgumentException("Prepare a nonempty pack first.");
            var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var item in items)
            {
                string name=item.Entry.Id+".json";
                if(item.Result==null||!item.Result.Success||!IsPackFileName(name)||!names.Add(name))throw new ArgumentException("Pack contains a failed or invalid item.");
                string path=Path.Combine(folder,name);if(File.Exists(path)||Directory.Exists(path))throw new IOException("Pack will not overwrite: "+path);
            }
            var ancestor=new DirectoryInfo(Path.GetFullPath(folder));
            while(ancestor!=null){if(ancestor.Exists&&(ancestor.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked pack folders are refused.");ancestor=ancestor.Parent;}
            Directory.CreateDirectory(folder);
            foreach(var item in items)
            {
                string json=JsonUtility.ToJson(LevelExport.ToLevelData(item.Result.Board,"Shape "+item.Entry.Number+": "+item.Entry.Shape),true);
                using(var stream=new FileStream(Path.Combine(folder,item.Entry.Id+".json"),FileMode.CreateNew,FileAccess.Write))
                using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(json);
            }
        }
        /// <summary>Human-readable evidence shown before confirmation.</summary>
        public static string Summary(IReadOnlyList<ShapePackItem> items)
        {
            var text=new StringBuilder("ID | shape | requested / trimmed | seed | arrows | coverage | score | tier | mask area | ms\n");
            foreach(var item in items){var r=item.Result;text.Append(item.Entry.Id).Append(" | ").Append(item.Entry.Shape).Append(" | ").Append(item.Entry.Width).Append('x').Append(item.Entry.Height);
                if(r==null){text.AppendLine(" | FAILED");continue;}
                text.Append(" / ").Append(r.Board.Width).Append('x').Append(r.Board.Height).Append(" | ").Append(r.Seed).Append(" | ").Append(r.Board.Arrows.Count).Append(" | ").Append(r.CoveragePermille).Append(" | ").Append(r.Rating.Score).Append(" | ").Append(r.Rating.Tier).Append(" | ").Append(r.Mask.Area).Append(" | ").Append(item.Milliseconds).AppendLine();}
            return text.ToString();
        }
    }
}
