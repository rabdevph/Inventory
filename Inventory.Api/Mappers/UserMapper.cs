using Inventory.Api.Models;
using Inventory.Shared.Dtos.Users;

namespace Inventory.Api.Mappers;

public static class UserMapper
{
    // Map ApplicationUser to ApplicationUserDto (single role)
    public static UserDto ToDto(this User user, string role = "")
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email ?? string.Empty,
            Firstname = user.Firstname,
            LastName = user.LastName,
            Department = user.Department,
            EmployeeCode = user.EmployeeCode,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Role = role
            // FullName is computed in the DTO
        };
    }

    // Map ApplicationUser to ApplicationUserSummaryDto (single role)
    public static UserSummaryDto ToSummaryDto(this User user, string role = "")
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Role = role
        };
    }

    // Map a collection of ApplicationUser to ApplicationUserDto list
    public static IEnumerable<UserDto> ToDto(this IEnumerable<User> users, IDictionary<string, string>? userRoles = null)
    {
        return users.Select(u => u.ToDto(userRoles != null && userRoles.ContainsKey(u.Id) ? userRoles[u.Id] : string.Empty));
    }

    // Map a collection of ApplicationUser to ApplicationUserSummaryDto list
    public static IEnumerable<UserSummaryDto> ToSummaryDto(this IEnumerable<User> users, IDictionary<string, string>? userRoles = null)
    {
        return users.Select(u => u.ToSummaryDto(userRoles != null && userRoles.ContainsKey(u.Id) ? userRoles[u.Id] : string.Empty));
    }

    // Map CreateUserDto to User entity
    public static User ToEntity(this CreateUserDto dto)
    {
        return new User
        {
            UserName = dto.UserName,
            Email = dto.Email,
            Firstname = dto.Firstname,
            LastName = dto.LastName,
            Department = dto.Department,
            EmployeeCode = dto.EmployeeCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Map UpdateUserDto to update an existing User entity
    public static void UpdateFromDto(this User user, UpdateUserDto dto)
    {
        user.UserName = dto.UserName;
        user.Email = dto.Email;
        user.Firstname = dto.Firstname;
        user.LastName = dto.LastName;
        user.Department = dto.Department;
        user.EmployeeCode = dto.EmployeeCode;
        user.UpdatedAt = DateTime.UtcNow;
    }
}