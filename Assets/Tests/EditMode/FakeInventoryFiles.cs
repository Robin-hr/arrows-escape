using System.Collections.Generic;
using System.IO;
using ArrowEscape.Data;
namespace ArrowEscape.Tests
{
    /// <summary>In-memory file-system protocol for atomic inventory tests.</summary>
    public sealed class FakeInventoryFiles : IInventoryFiles
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly Dictionary<string,string> Data=new Dictionary<string,string>();
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public readonly List<string> Calls=new List<string>();
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public bool FailReplace,FailMove;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public bool Exists(string path)=>Data.ContainsKey(path);
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public string Read(string path)=>Data[path];
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Write(string path,string value){Calls.Add("write:"+path);Data[path]=value;}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void CreateDirectory(string path){Calls.Add("mkdir:"+path);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Move(string a,string b){if(FailMove)throw new IOException("Move denied");if(Data.ContainsKey(b))throw new IOException("Exists");Calls.Add("move:"+b);Data[b]=Data[a];Data.Remove(a);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Replace(string a,string b){if(FailReplace)throw new IOException("Replace denied");Calls.Add("replace:"+b);Data[b]=Data[a];Data.Remove(a);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Delete(string path){Calls.Add("delete:"+path);Data.Remove(path);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Copy(string a,string b){if(Data.ContainsKey(b))throw new IOException("Exists");Data.Add(b,Data[a]);}
    }
}
