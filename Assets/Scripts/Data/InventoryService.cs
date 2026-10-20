using System;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.Data
{
    /// <summary>Persists each economic operation once, independently of progress.</summary>
    public sealed class InventoryService
    {
        private readonly IInventoryStore store;
        private readonly Action<string> warning;
        private bool warnedLoad,warnedSave;
        /// <summary>Pure rules over the loaded inventory.</summary>
        public ShopService Shop { get; private set; }
        /// <summary>The payload used by atomic storage.</summary>
        public Inventory State=>Shop.Inventory;
        /// <summary>Most recent load diagnostic.</summary>
        public InventoryLoadResult LoadResult { get; private set; }
        /// <summary>Whether the latest economic write failed.</summary>
        public bool LastSaveFailed { get; private set; }
        /// <summary>Injects storage and optional warning capture.</summary>
        public InventoryService(IInventoryStore store,Action<string> warning=null)
        {this.store=store??throw new ArgumentNullException(nameof(store));this.warning=warning??LogWarning;}
        private static void LogWarning(string message)=>Debug.LogWarning(message);
        /// <summary>Loads once for this initialization; repeated failures warn only once.</summary>
        public void Load()
        {
            try{LoadResult=store.Load()??new InventoryLoadResult(null,ProgressLoadStatus.ReadError,"No inventory result.");}
            catch(Exception error){LoadResult=new InventoryLoadResult(null,ProgressLoadStatus.ReadError,error.Message);}
            Shop=new ShopService(LoadResult.Inventory);
            if(LoadResult.Status!=ProgressLoadStatus.Loaded&&LoadResult.Status!=ProgressLoadStatus.NoFile&&!warnedLoad)
            {warnedLoad=true;warning("ArrowEscape inventory: "+LoadResult.Message);}
        }
        /// <summary>Awards and saves a new genuine clear.</summary>
        public int AwardLevelClear(string id,int stars)=>AwardLevelClear(id,stars,Shop.BeginAttempt());
        /// <summary>Persists a receipt and its coins before progress is allowed to save.</summary>
        public int AwardLevelClear(string id,int stars,int attemptId)
        {int before=State.LastPaidAttemptId;int reward=Shop.AwardLevelClear(id,stars,attemptId);if(before!=State.LastPaidAttemptId)Save();return reward;}
        /// <summary>Buys and saves once on success.</summary>
        public bool TryBuy(string id){if(!Shop.TryBuy(id))return false;Save();return true;}
        /// <summary>Equips and saves once on success.</summary>
        public bool TryEquip(string id){if(!Shop.TryEquip(id))return false;Save();return true;}
        /// <summary>Persists pending in-memory economic state.</summary>
        public bool Save()
        {
            try{LastSaveFailed=!store.TrySave(State);}catch(Exception){LastSaveFailed=true;}
            if(LastSaveFailed&&!warnedSave){warnedSave=true;warning("ArrowEscape inventory could not be saved. State remains in memory.");}
            return !LastSaveFailed;
        }
        /// <summary>Backs up before replacing development inventory; never resets when backup fails.</summary>
        public bool ResetWithBackup()
        {
            try{if(!store.TryBackup())return false;}catch(Exception){return false;}
            var previous=Shop;Shop=new ShopService();if(Save())return true;Shop=previous;return false;
        }
    }
}
