using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Api.Models;

/// <summary>
/// Represents a user in the inventory management system, extending ASP.NET Core Identity
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name
    /// </summary>
    public string Firstname { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Department or division where the user works (optional)
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Unique employee identifier/code assigned by the organization (optional)
    /// </summary>
    public string? EmployeeCode { get; set; }

    /// <summary>
    /// Indicates whether the user account is currently active and can access the system
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date and time when this user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when this user account was last updated (null if never updated)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the user's full name by combining first and last name.
    /// This is a computed property and is not stored in the database.
    /// </summary>
    [NotMapped]
    public string FullName => $"{Firstname} {LastName}".Trim();
}
