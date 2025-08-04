using Inventory.Shared.Enums;

namespace Inventory.Shared.Dtos.InventoryTransactions;

public class InventoryTransactionSummaryDto
{
    public int Id { get; set; }
    public string TransactionCode = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties for UI convenience
    public string DisplayName => $"{ItemName} (Transaction #{Id})";
    public string StatusBadge => Status == TransactionStatus.Completed ? "Completed"
                                : Status == TransactionStatus.Pending ? "Pending"
                                : Status == TransactionStatus.Cancelled ? "Cancelled" : Status.ToString();
    public string TypeBadge => Type == TransactionType.In ? "IN" : "OUT";
    public string DateDisplay => TransactionDate?.ToString("yyyy-MM-dd") ?? "N/A";
    public string CreatedDisplay => CreatedAt.ToString("yyyy-MM-dd");
}
