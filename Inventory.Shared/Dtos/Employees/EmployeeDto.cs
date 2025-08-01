namespace Inventory.Shared.DTOs.Employees;

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? EmployeeCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Computed properties for UI convenience
    public string DisplayName => $"{FullName} (Code: {EmployeeCode})";
    public string StatusBadge => IsActive ? "Active" : "Inactive";
}
