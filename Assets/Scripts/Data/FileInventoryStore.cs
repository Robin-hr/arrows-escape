using System;
using System.IO;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.Data
{
    /// <summary>Version-one atomic JSON inventory. Backups are unique and never overwritten.</summary>
    public sealed class FileInventoryStore : IInventoryStore
    {
        private readonly string path;
        private readonly IInventoryFiles files;
        private bool preserveOriginal;
        /// <summary>Takes an explicit path and optionally fake file operations.</summary>
        public FileInventoryStore(string fullPath,IInventoryFiles operations=null){path=fullPath;files=operations??new InventoryFiles();}
        /// <summary>Loads safely, renaming corrupt files before allowing defaults to be saved.</summary>
        public InventoryLoadResult Load()
        {
            preserveOriginal=false;
            try
            {
                if(!files.Exists(path))return new InventoryLoadResult(null,ProgressLoadStatus.NoFile);
                string text=files.Read(path);Inventory state;
                try
                {
                    if(!text.Trim().StartsWith("{")||!text.Trim().EndsWith("}"))throw new FormatException("Expected a JSON object.");
                    state=new Inventory{version=0};JsonUtility.FromJsonOverwrite(text,state);
                    if(state.version<1)throw new FormatException("Missing inventory version.");
                }
                catch(Exception error){return Corrupt(error.Message);}
                if(state.version>1){preserveOriginal=true;return new InventoryLoadResult(null,ProgressLoadStatus.NewerVersion,"Newer inventory retained untouched.");}
                state.Sanitize();return new InventoryLoadResult(state,ProgressLoadStatus.Loaded);
            }
            catch(Exception error){preserveOriginal=true;return new InventoryLoadResult(null,ProgressLoadStatus.ReadError,error.Message);}
        }
        private InventoryLoadResult Corrupt(string reason)
        {
            string backup=UniqueBackup("corrupt");
            try{files.Move(path,backup);}
            catch(Exception error){preserveOriginal=true;return new InventoryLoadResult(null,ProgressLoadStatus.ReadError,reason+" Backup failed: "+error.Message);}
            return new InventoryLoadResult(null,ProgressLoadStatus.Corrupt,reason+" Preserved at "+backup);
        }
        private string UniqueBackup(string kind)
        {
            string folder=Path.GetDirectoryName(path),stem=Path.GetFileNameWithoutExtension(path);int n=1;
            string candidate;
            do {candidate=Path.Combine(folder??"",stem+"."+kind+"-"+(n++)+".json");}while(files.Exists(candidate));
            return candidate;
        }
        /// <summary>Writes temp then replaces; any exception preserves the prior target.</summary>
        public bool TrySave(Inventory state)
        {
            string temp=path+".tmp";
            try
            {
                if(state==null||preserveOriginal)return false;
                state.version=1;files.CreateDirectory(Path.GetDirectoryName(path));files.Write(temp,JsonUtility.ToJson(state,true));
                if(files.Exists(path))files.Replace(temp,path);else files.Move(temp,path);
                return true;
            }
            catch(Exception){return false;}
            finally{try{if(files.Exists(temp))files.Delete(temp);}catch(Exception){}}
        }
        /// <summary>Preserves the current inventory before a deliberate reset; failure forbids resetting.</summary>
        public bool TryBackup()
        {try{if(files.Exists(path))files.Copy(path,UniqueBackup("backup"));return true;}catch(Exception){return false;}}
    }
}
