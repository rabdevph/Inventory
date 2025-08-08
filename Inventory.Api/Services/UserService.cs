using Inventory.Api.Common;
using Inventory.Api.Interfaces;
using Inventory.Api.Mappers;
using Inventory.Api.Models;
using Inventory.Shared.Dtos.Users;
using Inventory.Shared.DTOs.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services;

public class UserService(UserManager<User> userManager, ILogger<UserService> logger) : IUserService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<UserService> _logger = logger;

    #region Query Operations

    public async Task<ServiceResult<PagedResult<UserSummaryDto>>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        bool sortDescending = false)
    {
        // Validate pagination parameters to ensure they are within acceptable ranges
        if (page < 1)
            return ServiceResult<PagedResult<UserSummaryDto>>.BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return ServiceResult<PagedResult<UserSummaryDto>>.BadRequest("Page size must be between 1 and 100");

        var query = _userManager.Users.AsQueryable();

        // Filter by active status if provided
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        // Search by username, email, or full name
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(u =>
                (u.UserName ?? "").ToLower().Contains(lowerTerm) ||
                (u.Email ?? "").ToLower().Contains(lowerTerm) ||
                ((u.Firstname ?? "") + " " + (u.LastName ?? "")).ToLower().Contains(lowerTerm));
        }

        // Use the ApplySorting helper
        query = ApplySorting(query, null, sortDescending);

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync();

        // Apply pagination
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get roles for each user (single role per user)
        var userRoleDict = new Dictionary<string, string>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoleDict[user.Id] = roles.FirstOrDefault() ?? string.Empty;
        }

        // Apply role to users and project to DTOs
        var userDtos = users.Select(u => u.ToSummaryDto(userRoleDict.ContainsKey(u.Id) ? userRoleDict[u.Id] : string.Empty)).ToList();

        // Build paginated result with users and metadata
        var pagedResult = new PagedResult<UserSummaryDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<UserSummaryDto>>.Ok(pagedResult);
    }

    public async Task<ServiceResult<UserDto>> GetUserByIdAsync(string userId)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<UserDto>.BadRequest("User ID must be provided and cannot be empty");

        // Find user by using user ID
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        // Return not found if user doesn't exists
        if (user == null)
            return ServiceResult<UserDto>.NotFound($"User with ID {userId} not found");

        // Get user's role
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // Convert entity to DTO and return success result
        return ServiceResult<UserDto>.Ok(user.ToDto(role));
    }

    #endregion

    #region Modification Operations

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    // Clean and normalize the input data if needed (not shown here)

    {
        // Check if user with same username already exists
        var existingUsername = await _userManager.FindByNameAsync(createUserDto.UserName);
        if (existingUsername != null)
        {
            _logger.LogWarning("USER.CREATE.FAILED: Attempted to create user with duplicate username: {UserName}", createUserDto.UserName);
            return ServiceResult<UserDto>.Conflict("A user with the same username already exists");
        }

        // Check if user with same name(first name + last name) already exists
        var existingName = await _userManager.Users.FirstOrDefaultAsync(u =>
            (u.Firstname + u.LastName) == (createUserDto.Firstname + createUserDto.LastName));
        if (existingName != null)
        {
            _logger.LogWarning("USER.CREATE.FAILED: Attempted to create user with duplicate name: {FullName}",
                createUserDto.Firstname + " " + createUserDto.LastName);
            return ServiceResult<UserDto>.Conflict("A user with the same name (last and first) already exists");
        }

        // Check if user with same email already exists
        var existingEmail = await _userManager.FindByEmailAsync(createUserDto.Email);
        if (existingEmail != null)
        {
            _logger.LogWarning("USER.CREATE.FAILED: Attempted to create user with duplicate email: {Email}",
                createUserDto.Email);
            return ServiceResult<UserDto>.Conflict("A user with the same email already exists");
        }

        // Create new user entity
        var user = createUserDto.ToEntity();

        // Create user in the database
        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("USER.CREATE.FAILED: Failed to create user {UserName}. Reason: {Error}",
                user.UserName, errorMsg);
            return ServiceResult<UserDto>.BadRequest($"Failed to create user: {errorMsg}");
        }

        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, createUserDto.Role);
        if (!roleResult.Succeeded)
        {
            // Rollback user creation if role assignment fails
            await _userManager.DeleteAsync(user);
            var errorMsg = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            _logger.LogWarning("USER.CREATE.FAILED: Failed to assign role {Role} to user {UserName}. Reason: {Error}",
                createUserDto.Role, user.UserName, errorMsg);
            return ServiceResult<UserDto>.BadRequest($"Failed to assign role: {errorMsg}");
        }

        // Log successful creation for audit purposes
        _logger.LogInformation("USER.CREATE.SUCCESS: Created new user: {UserName} with ID {UserId}",
            user.UserName, user.Id);
        // Return created user DTO
        return ServiceResult<UserDto>.Ok(user.ToDto(createUserDto.Role), 201); // 201 Created
    }

    public async Task<ServiceResult<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<UserDto>.BadRequest("User ID must be provided and cannot be empty");

        // Fetch existing user to update
        var existingUser = await _userManager.FindByIdAsync(userId);
        if (existingUser == null)
            return ServiceResult<UserDto>.NotFound($"A user with ID {userId} does not exists");

        // Check if user with same username already exists
        var existingUsername = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id != existingUser.Id && u.UserName == updateUserDto.UserName);
        if (existingUsername != null)
        {
            _logger.LogWarning("USER.UPDATE.FAILED: Attempted to update user {UserId} with duplicate username: {UserName}",
                userId, updateUserDto.UserName);
            return ServiceResult<UserDto>.Conflict("A user with the same username already exists");
        }

        // Check if user with same name(first name + last name) already exists
        var existingName = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id != existingUser.Id && (u.Firstname + u.LastName) == (updateUserDto.Firstname + updateUserDto.LastName));
        if (existingName != null)
        {
            _logger.LogWarning("USER.UPDATE.FAILED: Attempted to update user {UserId} with duplicate name: {FullName}",
                userId, updateUserDto.Firstname + " " + updateUserDto.LastName);
            return ServiceResult<UserDto>.Conflict("A user with the same name (last and first) already exists");
        }

        // Check if user with same email already exists
        var existingEmail = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id != existingUser.Id && u.Email == updateUserDto.Email);
        if (existingEmail != null)
        {
            _logger.LogWarning("USER.UPDATE.FAILED: Attempted to update user {UserId} with duplicate email: {Email}",
                userId, updateUserDto.Email);
            return ServiceResult<UserDto>.Conflict("A user with the same email already exists");
        }

        // Update user fields
        existingUser.UpdateFromDto(updateUserDto);

        // Update user in the database
        var updateResult = await _userManager.UpdateAsync(existingUser);
        if (!updateResult.Succeeded)
        {
            var errorMsg = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            _logger.LogWarning("USER.UPDATE.FAILED: Failed to update user {UserId}. Reason: {Error}",
                userId, errorMsg);
            return ServiceResult<UserDto>.BadRequest($"Failed to update user: {errorMsg}");
        }

        // Handle role update if role is provided and different
        if (!string.IsNullOrWhiteSpace(updateUserDto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(existingUser);
            var currentRole = currentRoles.FirstOrDefault();
            if (currentRole != updateUserDto.Role)
            {
                if (!string.IsNullOrEmpty(currentRole))
                {
                    // Remove old role
                    var removeResult = await _userManager.RemoveFromRoleAsync(existingUser, currentRole);
                    if (!removeResult.Succeeded)
                    {
                        var errorMsg = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("USER.UPDATE.FAILED: Failed to remove old role {Role} from user {UserId}. Reason: {Error}",
                            currentRole, userId, errorMsg);
                        return ServiceResult<UserDto>.BadRequest($"Failed to remove old role: {errorMsg}");
                    }
                }

                // Add new role
                var addResult = await _userManager.AddToRoleAsync(existingUser, updateUserDto.Role);
                if (!addResult.Succeeded)
                {
                    var errorMsg = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("USER.UPDATE.FAILED: Failed to assign new role {Role} to user {UserId}. Reason: {Error}",
                        updateUserDto.Role, userId, errorMsg);
                    return ServiceResult<UserDto>.BadRequest($"Failed to assign new role: {errorMsg}");
                }
            }
        }

        // Log successful update for audit purposes
        _logger.LogInformation("USER.UPDATE.SUCCESS: Updated user: {UserName} with ID {UserId}",
            existingUser.UserName, existingUser.Id);
        // Return updated user DTO
        return ServiceResult<UserDto>.Ok(existingUser.ToDto(updateUserDto.Role));
    }

    public async Task<ServiceResult> DeactivateUserAsync(string userId)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.BadRequest("User ID must be provided and cannot be empty");

        // Find user by ID
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResult.NotFound($"User with ID {userId} not found");

        // If already deactivated, return conflict
        if (!user.IsActive)
            return ServiceResult.Conflict("User is already deactivated");

        // Deactivate user and update timestamp
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("USER.DEACTIVATE.SUCCESS: Deactivated user: {UserName} with ID {UserId}",
                user.UserName, user.Id);
            return ServiceResult.Ok();
        }

        // Return error if update failed
        var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("USER.DEACTIVATE.FAILED: Failed to deactivate user {UserName} with ID {UserId}. Reason: {Error}",
            user.UserName, user.Id, errorMsg);
        return ServiceResult.BadRequest($"Failed to deactivate user: {errorMsg}");
    }

    public async Task<ServiceResult> ActivateUserAsync(string userId)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.BadRequest("User ID must be provided and cannot be empty");

        // Find user by ID
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResult.NotFound($"User with ID {userId} not found");

        // If already active, return conflict
        if (user.IsActive)
            return ServiceResult.Conflict("User is already active");

        // Activate user and update timestamp
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("USER.ACTIVATE.SUCCESS: Activated user: {UserName} with ID {UserId}",
                user.UserName, user.Id);
            return ServiceResult.Ok();
        }

        // Return error if update failed
        var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("USER.ACTIVATE.FAILED: Failed to activate user {UserName} with ID {UserId}. Reason: {Error}",
            user.UserName, user.Id, errorMsg);
        return ServiceResult.BadRequest($"Failed to activate user: {errorMsg}");
    }

    public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult.BadRequest("User ID must be provided and cannot be empty");

        // Fetch existing
        var existingUser = await _userManager.FindByIdAsync(userId);
        if (existingUser == null)
            return ServiceResult.NotFound($"A user with ID {userId} does not exists");

        // Attempt to change password
        var result = await _userManager.ChangePasswordAsync(existingUser, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("USER.CHANGE_PASSWORD.SUCCESS: Changed password for user: {UserName} with ID {UserId}",
                existingUser.UserName, existingUser.Id);
            return ServiceResult.Ok();
        }

        // Log failure and return error details
        var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("USER.CHANGE_PASSWORD.FAILED: Failed to change password for user {UserName} with ID {UserId}. Reason: {Error}",
            existingUser.UserName, existingUser.Id, errorMsg);
        return ServiceResult.BadRequest($"Failed to change password: {errorMsg}");
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<UserDto>.BadRequest("User ID must be provided and cannot be empty");

        // Find user by ID
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        // Return not found if user doesn't exist
        if (user == null)
            return ServiceResult<UserDto>.NotFound($"User with ID {userId} not found");

        // Get user's role
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // Convert entity to DTO and return success result
        return ServiceResult<UserDto>.Ok(user.ToDto(role));
    }

    #endregion

    #region Private Helper Methods

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, bool sortDescending)
    {
        var validSortBy = sortBy?.Trim()?.ToLower();

        return validSortBy switch
        {
            "username" => sortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
            "email" => sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "fullname" => sortDescending
                ? query.OrderByDescending(u => u.Firstname).ThenByDescending(u => u.LastName)
                : query.OrderBy(u => u.Firstname).ThenBy(u => u.LastName),
            "createdat" => sortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => sortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName)
        };
    }

    #endregion
}
