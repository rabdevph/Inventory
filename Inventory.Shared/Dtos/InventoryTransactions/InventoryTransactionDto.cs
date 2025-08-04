using Inventory.Shared.Enums;

namespace Inventory.Shared.Dtos.InventoryTransactions;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReceivedByUser { get; set; } // For IN transaction
    public string? RequestedByEmployee { get; set; } // For OUT transaction
    public string? ProcessedByUser { get; set; } // For OUT transaction
    public string? CancelledByUser { get; set; } // For cancelled transaction
    public DateTime? CancelledAt { get; set; } // For cancelled transaction
    public string? Remarks { get; set; }

    // Computed properties for UI convenience
    public string DisplayName => $"{ItemName} (Transaction #{Id})";
    public string StatusBadge => Status == TransactionStatus.Completed ? "Completed"
                                : Status == TransactionStatus.Pending ? "Pending"
                                : Status == TransactionStatus.Cancelled ? "Cancelled" : Status.ToString();
    public string TypeBadge => Type == TransactionType.In ? "IN" : "OUT";
    public string DateDisplay => TransactionDate?.ToString("yyyy-MM-dd") ?? "N/A";
    public string CreatedDisplay => CreatedAt.ToString("yyyy-MM-dd");
    public string? CancelledDisplay => CancelledAt?.ToString("yyyy-MM-dd");
    public bool IsProcessed => Status == TransactionStatus.Completed;
}
