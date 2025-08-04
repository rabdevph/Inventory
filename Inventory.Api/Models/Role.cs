using Microsoft.AspNetCore.Identity;

namespace Inventory.Api.Models;

public class Role : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
