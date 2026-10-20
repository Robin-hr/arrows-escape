using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.Data
{
    /// <summary>Isolated volatile inventory for Editor callers and injected test fixtures.</summary>
    public sealed class MemoryInventoryStore : IInventoryStore
    {
        private string saved;
        /// <summary>Returns a detached payload.</summary>
        public InventoryLoadResult Load()=>new InventoryLoadResult(saved==null?null:JsonUtility.FromJson<Inventory>(saved),saved==null?ProgressLoadStatus.NoFile:ProgressLoadStatus.Loaded);
        /// <summary>Copies state without file access.</summary>
        public bool TrySave(Inventory state){saved=JsonUtility.ToJson(state);return true;}
        /// <summary>Volatile state needs no disk backup.</summary>
        public bool TryBackup()=>true;
    }
}
