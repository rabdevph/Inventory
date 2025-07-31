using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs.Items;

// Data transfer object for creating a new inventory item
public class CreateItemDto
{
    [Required(ErrorMessage = "Item name is required")]
    [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
    public string Unit { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Initial quantity must be non-negative")]
    public int InitialQuantity { get; set; } = 0;
}
