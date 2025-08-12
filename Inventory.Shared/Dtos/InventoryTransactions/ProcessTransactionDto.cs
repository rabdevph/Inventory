using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.InventoryTransactions;

public class ProcessTransactionDto
{
    [Required(ErrorMessage = "ProcessedByUserId is required")]
    [StringLength(450, ErrorMessage = "ProcessedByUserId cannot exceed 450 characters")]
    public string ProcessedByUserId { get; set; } = string.Empty;
}
