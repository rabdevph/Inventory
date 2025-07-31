using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Api.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Unit { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public bool CanReceiveStock => IsActive;

    [NotMapped]
    public bool CanDistribute => IsActive && Quantity > 0;
}
