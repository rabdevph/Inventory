using Inventory.Shared.Enums;

namespace Inventory.Api.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = new Item();
    public int Quantity { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? ReceivedByUserId { get; set; }
    public ApplicationUser? ReceivedByUser { get; set; }
    public int? RequestedByEmployeeId { get; set; }
    public Employee? RequestedByEmployee { get; set; }
    public string? ProcessedByUserId { get; set; }
    public ApplicationUser? ProcessedByUser { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}
