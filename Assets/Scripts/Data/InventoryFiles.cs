using System.IO;
using System.Text;
namespace ArrowEscape.Data
{
    /// <summary>Real file adapter, used only by runtime inventory storage.</summary>
    public sealed class InventoryFiles : IInventoryFiles
    {
        /// <summary>Reports existing files.</summary>
        public bool Exists(string path)
        {
            try{var attributes=File.GetAttributes(path);if((attributes&FileAttributes.Directory)!=0)throw new IOException("Inventory path is a directory.");return true;}
            catch(FileNotFoundException){return false;}catch(DirectoryNotFoundException){return false;}
        }
        /// <summary>Reads file contents.</summary>
        public string Read(string path)=>File.ReadAllText(path);
        /// <summary>Writes a temporary payload.</summary>
        public void Write(string path,string text)=>File.WriteAllText(path,text,new UTF8Encoding(false));
        /// <summary>Creates a parent directory.</summary>
        public void CreateDirectory(string path){if(!string.IsNullOrEmpty(path))Directory.CreateDirectory(path);}
        /// <summary>Moves without overwrite.</summary>
        public void Move(string source,string target)=>File.Move(source,target);
        /// <summary>Uses the profile's existing atomic replacement API.</summary>
        public void Replace(string source,string target)=>SafeFile.Replace(source,target);
        /// <summary>Removes a leftover temporary file.</summary>
        public void Delete(string path)=>File.Delete(path);
        /// <summary>Copies without overwrite.</summary>
        public void Copy(string source,string target)=>File.Copy(source,target);
    }
}
