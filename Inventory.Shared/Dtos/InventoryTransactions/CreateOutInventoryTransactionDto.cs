using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.InventoryTransactions;

public class CreateOutInventoryTransactionDto
{
    [Required]
    public int ItemId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    [Required]
    public int RequestedByEmployeeId { get; set; }

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
    public string? Remarks { get; set; }
}
