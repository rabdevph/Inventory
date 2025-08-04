using Inventory.Shared.Enums;

namespace Inventory.Api.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }
    public int? RequestedByEmployeeId { get; set; }
    public Employee? RequestedByEmployee { get; set; }
    public string? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }
    public string? CancelledByUserId { get; set; }
    public User? CancelledByUser { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Remarks { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
