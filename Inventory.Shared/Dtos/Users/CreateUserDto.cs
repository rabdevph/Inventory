using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.Users;

public class CreateUserDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string Firstname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Department cannot exceed 200 characters.")]
    public string? Department { get; set; }

    [StringLength(50, ErrorMessage = "Employee code cannot exceed 50 characters.")]
    public string? EmployeeCode { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters and at most 100 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}
