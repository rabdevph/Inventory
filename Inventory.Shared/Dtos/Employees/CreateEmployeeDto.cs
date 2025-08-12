using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.Employees;

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
    public string? Position { get; set; }

    [StringLength(200, ErrorMessage = "Department cannot exceed 200 characters")]
    public string? Department { get; set; }

    [StringLength(50, ErrorMessage = "Employee code cannot exceed 50 characters")]
    public string? EmployeeCode { get; set; }
}
