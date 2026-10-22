using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure wallet, receipt, ownership and equipment rules over one inventory.</summary>
    public sealed class ShopService
    {
        /// <summary>The shared payload saved by the data service.</summary>
        public Inventory Inventory { get; }
        /// <summary>Raised once when the numeric balance changes.</summary>
        public event Action<int> CoinsChanged;
        /// <summary>Raised once after a successful purchase.</summary>
        public event Action<string> ItemPurchased;
        /// <summary>Raised once when equipment changes in a slot.</summary>
        public event Action<ShopSlot,string> EquippedChanged;
        /// <summary>Wraps and normalizes an inventory.</summary>
        public ShopService(Inventory inventory=null){Inventory=inventory??new Inventory();Inventory.Sanitize();}
        /// <summary>Issues a fresh monotonic attempt without saving mid-level state.</summary>
        public int BeginAttempt()
        {
            long next=Math.Max(1L,Math.Max(Inventory.NextAttemptId,(long)Inventory.LastPaidAttemptId+1));
            if(next>=int.MaxValue)throw new InvalidOperationException("Attempt ids exhausted; refusing to reuse a paid receipt.");
            Inventory.NextAttemptId=(int)next+1;return (int)next;
        }
        /// <summary>A genuine clear with a newly issued receipt.</summary>
        public int AwardLevelClear(string levelId,int stars)=>AwardLevelClear(levelId,stars,BeginAttempt());
        /// <summary>Applies a receipt once; duplicate or older attempts never award coins.</summary>
        public int AwardLevelClear(string levelId,int stars,int attemptId)
        {
            if(string.IsNullOrWhiteSpace(levelId))throw new ArgumentException("A level id is required.",nameof(levelId));
            if(stars<0||stars>3)throw new ArgumentOutOfRangeException(nameof(stars));
            if(attemptId<=Inventory.LastPaidAttemptId)return 0;
            if(attemptId==int.MaxValue)throw new ArgumentOutOfRangeException(nameof(attemptId));
            LevelRewardRecord record=null;
            foreach(var candidate in Inventory.RewardedStars)if(candidate!=null&&candidate.id==levelId){record=candidate;break;}
            int previous=record==null?0:Math.Max(0,Math.Min(3,record.stars));
            int reward=CoinReward.Compute(record!=null,previous,stars);
            if(record==null){record=new LevelRewardRecord{id=levelId};Inventory.RewardedStars.Add(record);}
            record.stars=Math.Max(previous,stars);Inventory.LastPaidAttemptId=attemptId;
            Inventory.NextAttemptId=Math.Max(Inventory.NextAttemptId,attemptId+1);
            int before=Inventory.Coins;SetCoins((long)before+reward);return Inventory.Coins-before;
        }
        /// <summary>Whether the wallet can cover a nonnegative price.</summary>
        public bool CanAfford(int price)=>price>=0&&Inventory.Coins>=price;
        /// <summary>Checks affordability of an available, known item.</summary>
        public bool CanAfford(string id){var item=ShopCatalog.Find(id);return item!=null&&item.IsAvailable&&CanAfford(item.Price);}
        /// <summary>Unknown retained ids have no active ownership semantics.</summary>
        public bool IsOwned(string id)=>ShopCatalog.Find(id)!=null&&Inventory.OwnedItemIds.Contains(id);
        /// <summary>Whether a known item occupies its equipment slot.</summary>
        public bool IsEquipped(string id){var item=ShopCatalog.Find(id);return item!=null&&IsOwned(id)&&Inventory.EquippedIn(item.Slot)==id;}
        /// <summary>Buys once, without equipping automatically.</summary>
        public bool TryBuy(string id)
        {
            var item=ShopCatalog.Find(id);
            if(item==null||!item.IsAvailable||IsOwned(id)||!CanAfford(item.Price))return false;
            Inventory.OwnedItemIds.Add(id);SetCoins((long)Inventory.Coins-item.Price);ItemPurchased?.Invoke(id);return true;
        }
        /// <summary>Explicit-slot overload rejects mismatched equipment.</summary>
        public bool TryEquip(ShopSlot slot,string id){var item=ShopCatalog.Find(id);return item!=null&&item.Slot==slot&&TryEquip(id);}
        /// <summary>Equips an owned item in its matching slot.</summary>
        public bool TryEquip(string id)
        {
            var item=ShopCatalog.Find(id);if(item==null||!IsOwned(id))return false;
            if(IsEquipped(id))return true;
            Inventory.SetEquipped(item.Slot,id);
            EquippedChanged?.Invoke(item.Slot,id);return true;
        }
        /// <summary>Adds development grants or other explicit integer rewards, capped safely.</summary>
        public void AddCoins(int amount){if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));SetCoins((long)Inventory.Coins+amount);}
        private void SetCoins(long value)
        {int next=(int)Math.Max(0,Math.Min(EconomyConfig.MaxCoins,value));int previous=Inventory.Coins;Inventory.Coins=next;Inventory.ReleaseRetirementCredit();if(previous!=Inventory.Coins)CoinsChanged?.Invoke(Inventory.Coins);}
    }
}
