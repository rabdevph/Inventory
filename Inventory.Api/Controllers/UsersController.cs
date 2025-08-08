
using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Interfaces;
using Inventory.Api.Common;
using Inventory.Shared.Dtos.Users;
using Inventory.Shared.DTOs.Common;
using System.Net;

namespace Inventory.Api.Controllers;

/// <summary>
/// API Controller for managing users.
/// Provides CRUD operations for users including creation, retrieval, updating, activation, deactivation, password change, and current user details.
/// </summary>
/// <remarks>
/// This controller handles all user-related operations in the inventory management system.
/// All operations return standardized responses using the ServiceResult pattern for consistent error handling.
/// </remarks>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Tags("Users")]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ApiBaseController
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UsersController> _logger = logger;

    /// <summary>
    /// Retrieves all users with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of users per page (default: 20, range: 1-100)</param>
    /// <param name="isActive">Filter by active status. null = all users, true = active only, false = inactive only</param>
    /// <param name="searchTerm">Search term to filter by username, email, or full name (case-insensitive)</param>
    /// <param name="sortDescending">Sort direction. true = descending, false = ascending (default: false)</param>
    /// <returns>A paginated list of user summaries matching the specified criteria.</returns>
    /// <response code="200">Returns the paginated list of users</response>
    /// <response code="400">Invalid parameters provided (e.g., invalid page number, page size out of range)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/users?page=1&amp;pageSize=10&amp;isActive=true&amp;searchTerm=admin&amp;sortDescending=false
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool sortDescending = false)
    {
        var result = await _userService.GetAllUsersAsync(page, pageSize, isActive, searchTerm, sortDescending);
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve</param>
    /// <returns>The requested user details if found, otherwise a 404 Not Found response.</returns>
    /// <response code="200">Returns the requested user details</response>
    /// <response code="400">Invalid ID parameter</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/users/123
    /// </example>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <param name="createUserDto">The user data for creation. All required fields must be provided and valid. Usernames and emails must be unique.</param>
    /// <returns>The newly created user with assigned ID and system-generated fields.</returns>
    /// <response code="201">User was successfully created</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="409">A user with the specified username or email already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users
    ///     {
    ///         "userName": "admin",
    ///         "email": "admin@example.com",
    ///         "firstname": "System",
    ///         "lastName": "Administrator",
    ///         "department": "IT",
    ///         "employeeCode": "EMP001",
    ///         "password": "P@ssw0rd!",
    ///         "role": "Admin"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        _logger.LogInformation("API.USERS.CREATE: Creating new user: {UserName}", createUserDto?.UserName);

        if (createUserDto == null)
        {
            _logger.LogWarning("API.USERS.CREATE.VALIDATION: Request body is null for user creation");
            var validationResult = ServiceResult.ValidationError("Request body cannot be null.");
            return HandleServiceResult(validationResult);
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.USERS.CREATE.VALIDATION: Model validation failed for user creation - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _userService.CreateUserAsync(createUserDto);
        if (!result.Success)
        {
            _logger.LogWarning("API.USERS.CREATE.FAILED: Failed to create user '{UserName}' - Error: {ErrorMessage}", createUserDto.UserName, result.ErrorMessage);
        }
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Updates an existing user with new information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="updateUserDto">The updated user data. Only provided fields will be modified. Usernames and emails must remain unique.</param>
    /// <returns>The updated user with all current information including modified timestamps.</returns>
    /// <response code="200">User was successfully updated</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="409">Another user with the specified username or email already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/users/123
    ///     {
    ///         "userName": "updateduser",
    ///         "email": "updated@example.com",
    ///         "firstname": "Updated",
    ///         "lastName": "User",
    ///         "department": "HR",
    ///         "employeeCode": "EMP002",
    ///         "role": "User"
    ///     }
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateUserDto)
    {
        _logger.LogInformation("API.USERS.UPDATE: Updating user ID: {UserId} with username: {UserName}", id, updateUserDto?.UserName);

        if (updateUserDto == null)
        {
            _logger.LogWarning("API.USERS.UPDATE.VALIDATION: Request body is null for user update ID: {UserId}", id);

            var validationResult = ServiceResult.ValidationError("Request body cannot be null.");
            return HandleServiceResult(validationResult);
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.USERS.UPDATE.VALIDATION: Model validation failed for user update ID: {UserId} - Errors: {ValidationErrors}", id, validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        if (!result.Success)
        {
            _logger.LogWarning("API.USERS.UPDATE.FAILED: Failed to update user ID: {UserId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Deactivates (soft deletes) a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to deactivate</param>
    /// <returns>Success if the user was deactivated, otherwise an error response.</returns>
    /// <response code="200">User was successfully deactivated</response>
    /// <response code="400">Invalid ID parameter</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="409">User is already deactivated</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPatch("{id}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeactivateUser(string id)
    {
        _logger.LogInformation("API.USERS.DEACTIVATE: Deactivating user ID: {UserId}", id);

        var result = await _userService.DeactivateUserAsync(id);
        if (!result.Success)
        {
            _logger.LogWarning("API.USERS.DEACTIVATE.FAILED: Failed to deactivate user ID: {UserId} - Error: {ErrorMessage}",
                id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Activates a previously deactivated user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to activate</param>
    /// <returns>Success if the user was activated, otherwise an error response.</returns>
    /// <response code="200">User was successfully activated</response>
    /// <response code="400">Invalid ID parameter</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="409">User is already active</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPatch("{id}/activate")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> ActivateUser(string id)
    {
        _logger.LogInformation("API.USERS.ACTIVATE: Activating user ID: {UserId}", id);

        var result = await _userService.ActivateUserAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.USERS.ACTIVATE.FAILED: Failed to activate user ID: {UserId} - Error: {ErrorMessage}",
                id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Changes the password for a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose password is to be changed</param>
    /// <param name="changePasswordDto">The password change request containing the current, new, and confirmation password</param>
    /// <returns>Success if the password was changed, otherwise an error response.</returns>
    /// <response code="200">Password was successfully changed</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/123/change-password
    ///     {
    ///         "currentPassword": "oldPass123",
    ///         "newPassword": "newPass456",
    ///         "confirmPassword": "newPass456"
    ///     }
    /// </remarks>
    [HttpPost("{id}/change-password")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> ChangePassword(
        string id,
        [FromBody] ChangePasswordDto changePasswordDto)
    {
        _logger.LogInformation("API.USERS.CHANGE_PASSWORD: Changing password for user ID: {UserId}", id);

        if (changePasswordDto == null)
        {
            _logger.LogWarning("API.USERS.CHANGE_PASSWORD.VALIDATION: Request body is null for password change ID: {UserId}", id);
            var validationResult = ServiceResult.ValidationError("Request body cannot be null.");
            return HandleServiceResult(validationResult);
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.USERS.CHANGE_PASSWORD.VALIDATION: Model validation failed for password change ID: {UserId} - Errors: {ValidationErrors}",
                id, validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
        if (!result.Success)
        {
            _logger.LogWarning("API.USERS.CHANGE_PASSWORD.FAILED: Failed to change password for user ID: {UserId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Gets the details of the currently logged-in user.
    /// </summary>
    /// <returns>The details of the currently authenticated user.</returns>
    /// <response code="200">Returns the current user details</response>
    /// <response code="400">User ID not found in claims</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/users/me
    /// </example>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // You may want to get the userId from the claims in a real app
        var userId = User?.FindFirst("sub")?.Value ?? User?.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User ID not found in claims.");

        var result = await _userService.GetCurrentUserAsync(userId);
        return HandleServiceResult(result);
    }
}
