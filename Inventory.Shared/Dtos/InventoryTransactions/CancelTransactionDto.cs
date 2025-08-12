using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.InventoryTransactions;

public class CancelTransactionDto
{
    [Required(ErrorMessage = "CancelledByUserId is required")]
    [StringLength(450, ErrorMessage = "CancelledByUserId cannot exceed 450 characters")]
    public string CancelledByUserId { get; set; } = string.Empty;
}
