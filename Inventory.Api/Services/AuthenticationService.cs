using Inventory.Api.Common;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Mappers;
using Inventory.Api.Models;
using Inventory.Shared.Dtos.Auth;
using Inventory.Shared.Dtos.Users;
using Microsoft.AspNetCore.Identity;

namespace Inventory.Api.Services;

public class AuthenticationService(
    InventoryContext context,
    ILogger<AuthenticationService> logger,
    UserManager<User> userManager,
    SignInManager<User> signInManager) : IAuthenticationService
{
    private readonly InventoryContext _context = context;
    private readonly ILogger<AuthenticationService> _logger = logger;
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;

    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginDto loginDto)
    {
        // Find user by username
        var user = await _userManager.FindByNameAsync(loginDto.Username);
        if (user == null)
        {
            _logger.LogWarning("AUTH.LOGIN.FAILED: Login attempt with invalid username: {Username}", loginDto.Username);
            return ServiceResult<LoginResponseDto>.Unauthorized("Invalid username or password");
        }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("AUTH.LOGIN.FAILED: Failed login attempt for user: {Username}", loginDto.Username);
            return ServiceResult<LoginResponseDto>.Unauthorized("Invalid username or password");
        }

        // Sign in the user (creates new authentication cookie)
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation("AUTH.LOGIN.SUCCESS: User {Username} logged in successfully", user.UserName);

        var userRoles = await _userManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault();
        if (string.IsNullOrEmpty(userRole))
        {
            _logger.LogError("AUTH.LOGIN.FAILED: User {UserId} has no assigned role", user.Id);
            return ServiceResult<LoginResponseDto>.InternalError("User role configuration error");
        }

        var response = new LoginResponseDto
        {
            User = user.ToDto(userRole!),
            Message = "Login successful"
        };

        return ServiceResult<LoginResponseDto>.Ok(response);
    }

    public async Task<ServiceResult> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("AUTH.LOGOUT.SUCCESS: User logged out successfully");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(_signInManager.Context.User);
        if (user == null)
            return ServiceResult<UserDto>.Unauthorized("User not authenticated");

        var userRoles = await _userManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault();
        if (string.IsNullOrEmpty(userRole))
        {
            _logger.LogError("AUTH.GETUSER.FAILED: User {UserId} has no assigned role", user.Id);
            return ServiceResult<UserDto>.InternalError("User role configuration error");
        }

        var response = user.ToDto(userRole!);
        return ServiceResult<UserDto>.Ok(response);
    }
}
