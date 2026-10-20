using System;
using System.Globalization;
using System.IO;
using ArrowEscape.Core;
namespace ArrowEscape.Editor
{
    /// <summary>Lexical asset-folder protection and non-overwriting export path selection.</summary>
    public static class LevelExportPaths
    {
        /// <summary>Generated resources stay outside both hand-authored level folders.</summary>
        public const string DefaultFolder="Assets/Resources/GeneratedLevels";
        /// <summary>Returns an error or null; accepts asset-relative folders only.</summary>
        public static string Validate(string assetFolder)
        {
            if(string.IsNullOrWhiteSpace(assetFolder))return "Choose a folder under Assets/.";
            string folder=assetFolder.Replace('\\','/').TrimEnd('/');
            if(!folder.Equals("Assets",StringComparison.Ordinal)&&!folder.StartsWith("Assets/",StringComparison.Ordinal))
                return "Export must stay inside Assets/.";
            foreach(string part in folder.Split('/'))
                if(part=="."||part==".."||part.Length==0||part.EndsWith(".",StringComparison.Ordinal)||
                    part.EndsWith(" ",StringComparison.Ordinal)||part.IndexOfAny(Path.GetInvalidFileNameChars())>=0)
                    return "Export folder contains an invalid or traversing path segment.";
            foreach(string protectedFolder in new[]{"Assets/Resources/Levels","Assets/Resources/TestLevels"})
                if(folder.Equals(protectedFolder,StringComparison.OrdinalIgnoreCase)||folder.StartsWith(protectedFolder+"/",StringComparison.OrdinalIgnoreCase))
                    return "Hand-authored Levels and TestLevels are protected.";
            return null;
        }
        /// <summary>Uses invariant decimal numbers, dimensions, tier and seed.</summary>
        public static string BuildFileName(int width,int height,DifficultyTier tier,int seed)
            =>string.Format(CultureInfo.InvariantCulture,"gen_{0}x{1}_{2}_{3}.json",width,height,tier,seed);
        /// <summary>Works with arbitrary folders, including test temp folders; never chooses an existing file/directory.</summary>
        public static string GetUniquePath(string folder,string fileName)
        {
            if(string.IsNullOrEmpty(folder))throw new ArgumentException("A folder is required.",nameof(folder));
            if(string.IsNullOrEmpty(fileName)||Path.GetFileName(fileName)!=fileName||fileName.IndexOf('\\')>=0)
                throw new ArgumentException("A simple file name is required.",nameof(fileName));
            string stem=Path.GetFileNameWithoutExtension(fileName),extension=Path.GetExtension(fileName);
            string path=Path.Combine(folder,fileName);int suffix=2;
            while(File.Exists(path)||Directory.Exists(path))path=Path.Combine(folder,stem+"_"+(suffix++)+extension);
            return path.Replace('\\','/');
        }
    }
}

