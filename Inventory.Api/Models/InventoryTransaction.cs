using Inventory.Shared.Enums;

namespace Inventory.Api.Models;

/// <summary>
/// Represents a transaction record for inventory item movements, tracking all ins and outs of inventory items
/// </summary>
public class InventoryTransaction
{
    /// <summary>
    /// Unique identifier for the inventory transaction
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key reference to the inventory item involved in this transaction
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Navigation property to the associated inventory item
    /// </summary>
    public Item Item { get; set; } = new Item();

    /// <summary>
    /// Number of items involved in this transaction (positive for incoming, negative for outgoing)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Type of transaction (IN, OUT, ADJUSTMENT, etc.)
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// Date and time when the transaction occurred
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// User ID of the person who physically received the items (applicable for incoming transactions)
    /// </summary>
    public string? ReceivedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who received the items
    /// </summary>
    public ApplicationUser? ReceivedByUser { get; set; }

    /// <summary>
    /// User ID of the person who requested the items (applicable for outgoing transactions)
    /// </summary>
    public string? RequestedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who requested the items
    /// </summary>
    public ApplicationUser? RequestedByUser { get; set; }

    /// <summary>
    /// User ID of the person who processed/approved this transaction
    /// </summary>
    public string? ProcessedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who processed this transaction
    /// </summary>
    public ApplicationUser? ProcessedByUser { get; set; }

    /// <summary>
    /// Additional notes or comments about this transaction
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Date and time when this transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
