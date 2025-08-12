using Inventory.Api.Common;
using Inventory.Shared.Dtos.Auth;
using Inventory.Shared.Dtos.Users;

namespace Inventory.Api.Interfaces;

public interface IAuthenticationService
{
    Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginDto loginDto);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult<UserDto>> GetCurrentUserAsync();
}
