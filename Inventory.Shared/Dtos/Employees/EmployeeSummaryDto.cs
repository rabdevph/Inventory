namespace Inventory.Shared.Dtos.Employees;

// Simplified data transfer object for employee summaries in lists
public class EmployeeSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? EmployeeCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties for UI convenience
    public string DisplayName => $"{FullName} (Code: {EmployeeCode})";
}
