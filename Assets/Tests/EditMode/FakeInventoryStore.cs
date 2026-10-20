using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Detached inventory snapshots and call counts, never real player files.</summary>
    public sealed class FakeInventoryStore : IInventoryStore
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public Inventory Saved;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public int Saves,Loads,Backups;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public bool FailSave,FailBackup;
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public Action BeforeSave;
        /// <summary>Returns a JSON copy so a crash really discards unsaved memory.</summary>
        public InventoryLoadResult Load(){Loads++;return new InventoryLoadResult(Saved==null?null:Copy(Saved),Saved==null?ProgressLoadStatus.NoFile:ProgressLoadStatus.Loaded);}
        /// <summary>Commits only when failure is disabled.</summary>
        public bool TrySave(Inventory state){BeforeSave?.Invoke();Saves++;if(FailSave)return false;Saved=Copy(state);return true;}
        /// <summary>Records development backups.</summary>
        public bool TryBackup(){Backups++;return !FailBackup;}
        private static Inventory Copy(Inventory state)=>JsonUtility.FromJson<Inventory>(JsonUtility.ToJson(state));
    }
}
