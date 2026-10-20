namespace ArrowEscape.Core
{
    /// <summary>Immutable cosmetic catalog entry, without rendering or gameplay behavior.</summary>
    public sealed class ShopItem
    {
        /// <summary>Permanent released id.</summary>
        public string Id { get; }
        /// <summary>Display name.</summary>
        public string Name { get; }
        /// <summary>Future visual description.</summary>
        public string Description { get; }
        /// <summary>Equipment slot.</summary>
        public ShopSlot Slot { get; }
        /// <summary>Ordered shop section.</summary>
        public string Category { get; }
        /// <summary>Integer coin cost.</summary>
        public int Price { get; }
        /// <summary>Primary hexadecimal tint.</summary>
        public string PrimaryColor { get; }
        /// <summary>Secondary hexadecimal tint.</summary>
        public string SecondaryColor { get; }
        /// <summary>Whether purchasing is released.</summary>
        public bool IsAvailable { get; }
        /// <summary>Creates a catalog entry.</summary>
        public ShopItem(string id, string name, string description, ShopSlot slot, string category, int price, string primary, string secondary, bool available)
        { Id=id;Name=name;Description=description;Slot=slot;Category=category;Price=price;PrimaryColor=primary;SecondaryColor=secondary;IsAvailable=available; }
    }
}
