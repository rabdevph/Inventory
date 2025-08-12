namespace Inventory.Shared.Dtos.Items;

// Simplified data transfer object for inventory item summaries in lists
public class ItemSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public bool CanDistribute { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties for list display
    public string StockStatus => Quantity > 0 ? $"{Quantity}" : "0";
    public bool IsLowStock => Quantity > 0 && Quantity <= 10;
    public string StockLevelClass => Quantity == 0 ? "text-danger" : IsLowStock ? "text-warning" : "text-success";
    public string StatusIcon => IsActive ? "✓" : "✗";
}
