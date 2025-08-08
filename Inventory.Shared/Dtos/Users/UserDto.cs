namespace Inventory.Shared.Dtos.Users;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? EmployeeCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Role { get; set; } = string.Empty;

    // Computed properties for UI convenience
    public string FullName => $"{Firstname} {LastName}".Trim();
    public string StatusLabel => IsActive ? "Active" : "Inactive";
}
