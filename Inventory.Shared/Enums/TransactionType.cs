using System.Text.Json.Serialization;

namespace Inventory.Shared.Enums;

/// <summary>
/// Defines the types of inventory transactions that can occur in the system
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    /// <summary>
    /// Incoming transaction - adds items to inventory (e.g., purchases, returns, stock receipts)
    /// </summary>
    In,
    
    /// <summary>
    /// Outgoing transaction - removes items from inventory (e.g., sales, distributions, consumption)
    /// </summary>
    Out,
    
    /// <summary>
    /// Inventory adjustment - corrects stock levels (e.g., physical count corrections, damage, loss)
    /// </summary>
    Adjustment
}
