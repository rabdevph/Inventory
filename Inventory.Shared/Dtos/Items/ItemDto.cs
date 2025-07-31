namespace Inventory.Shared.DTOs.Items;

// Data transfer object for returning inventory item details
public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public bool CanReceiveStock { get; set; }
    public bool CanDistribute { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Computed properties for UI convenience
    public string DisplayName => $"{Name} (ID: {Id})";
    public string StockStatus => Quantity > 0 ? $"{Quantity} in stock" : "Out of stock";
    public string StatusBadge => IsActive ? "Active" : "Inactive";
    public bool IsLowStock => Quantity > 0 && Quantity <= 10;
    public string StockLevelClass => Quantity == 0 ? "danger" : IsLowStock ? "warning" : "success";
}
