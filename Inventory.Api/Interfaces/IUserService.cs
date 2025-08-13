using Inventory.Api.Common;
using Inventory.Shared.Dtos.Users;
using Inventory.Shared.Dtos.Common;

namespace Inventory.Api.Interfaces;

public interface IUserService
{
    Task<ServiceResult<PagedResult<UserSummaryDto>>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        string? sortBy = "UserName",
        bool sortDescending = false
    );
    Task<ServiceResult<UserDto>> GetUserByIdAsync(string userId);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
    Task<ServiceResult> DeactivateUserAsync(string userId);
    Task<ServiceResult> ActivateUserAsync(string userId);
    Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId);
}
