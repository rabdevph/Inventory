using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.Users;

public class UpdateUserDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Firstname { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Department { get; set; }

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}
