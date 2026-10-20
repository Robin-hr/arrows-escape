using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Inventory load diagnostic, using the same status vocabulary as progress.</summary>
    public sealed class InventoryLoadResult
    {
        /// <summary>Non-null state for continued play.</summary>
        public Inventory Inventory { get; }
        /// <summary>Load disposition.</summary>
        public ProgressLoadStatus Status { get; }
        /// <summary>Diagnostic or backup path.</summary>
        public string Message { get; }
        /// <summary>Creates a detached store result.</summary>
        public InventoryLoadResult(Inventory inventory,ProgressLoadStatus status,string message="")
        {Inventory=inventory??new Inventory();Status=status;Message=message;}
    }
}
