using Microsoft.AspNetCore.Identity;

namespace Inventory.Api.Models;

/// <summary>
/// Represents a custom role in the inventory management system, extending ASP.NET Core Identity roles
/// with additional properties for enhanced role management
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Description of the role's purpose, permissions, or responsibilities within the system
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this role is currently active and can be assigned to users.
    /// Inactive roles are retained for audit purposes but cannot be used for new assignments.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
