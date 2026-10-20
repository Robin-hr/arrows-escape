using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Independent inventory persistence with an explicit safe reset backup.</summary>
    public interface IInventoryStore
    {
        /// <summary>Loads defaults or a preserved version-one payload.</summary>
        InventoryLoadResult Load();
        /// <summary>Atomically saves a complete snapshot.</summary>
        bool TrySave(Inventory inventory);
        /// <summary>Copies the current file before a deliberate development reset.</summary>
        bool TryBackup();
    }
}
