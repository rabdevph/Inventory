using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Api.Models;

/// <summary>
/// Represents an inventory item that can be tracked and managed in the system
/// </summary>
public class Item
{
    /// <summary>
    /// Unique identifier for the inventory item
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name or title of the inventory item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the item (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Unit of measurement for this item (e.g., pieces, boxes, liters)
    /// TODO: Consider making this an enum or reference to a Units table
    /// </summary>
    public int Unit { get; set; }

    /// <summary>
    /// Current quantity/stock level of this item in inventory
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Indicates whether this item is currently active and available for transactions.
    /// Inactive items are hidden from regular operations but preserve transaction history.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date and time when this item was first created in the system
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when this item was last updated (null if never updated)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets whether this item is available for new incoming transactions (purchases, restocking).
    /// This is a computed property and is not stored in the database.
    /// </summary>
    [NotMapped]
    public bool CanReceiveStock => IsActive;

    /// <summary>
    /// Gets whether this item is available for outgoing transactions (sales, distributions).
    /// This is a computed property and is not stored in the database.
    /// </summary>
    [NotMapped]
    public bool CanDistribute => IsActive && Quantity > 0;
}
