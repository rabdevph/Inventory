using Inventory.Api.Common;
using Inventory.Api.Interfaces;
using Inventory.Shared.Dtos.Auth;
using Inventory.Shared.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory.Api.Controllers;

/// <summary>
/// API Controller for managing user authentication and authorization.
/// Provides secure authentication operations including login, logout, and current user retrieval.
/// Supports cookie-based authentication with ASP.NET Core Identity integration.
/// </summary>
/// <remarks>
/// This controller handles all authentication-related operations in the inventory management system.
/// All operations return standardized responses using the ServiceResult pattern for consistent error handling.
/// 
/// Key Features:
/// - Secure user login with username/password validation
/// - Session-based authentication with secure cookies
/// - User logout with proper session cleanup
/// - Current authenticated user information retrieval
/// - Comprehensive security logging and audit trail
/// - OpenAPI/Swagger documentation for authentication workflows
/// </remarks>
/// <remarks>
/// Initializes a new instance of the AuthController.
/// </remarks>
/// <param name="authenticationService">The authentication service for user authentication operations</param>
/// <param name="logger">Logger instance for this controller</param>
/// <exception cref="ArgumentNullException">Thrown when authenticationService or logger is null</exception>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController(
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger) : ApiBaseController
{
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>
    /// Authenticates a user with username and password credentials.
    /// </summary>
    /// <param name="loginDto">Login credentials containing username and password</param>
    /// <returns>
    /// Login response containing user information and authentication token/session data.
    /// </returns>
    /// <response code="200">User successfully authenticated. Returns user details and session information.</response>
    /// <response code="400">Invalid request data or missing required fields</response>
    /// <response code="401">Invalid username or password credentials</response>
    /// <response code="422">Validation errors in the request data</response>
    /// <response code="500">Internal server error during authentication process</response>
    /// <example>
    /// POST /api/auth/login
    /// {
    ///   "username": "admin",
    ///   "password": "SecurePassword123"
    /// }
    /// </example>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        _logger.LogInformation("API.AUTH.LOGIN: Login attempt for username: {Username}", loginDto?.Username ?? "null");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.AUTH.LOGIN.VALIDATION: Model validation failed for login - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _authenticationService.LoginAsync(loginDto!);
        if (!result.Success)
        {
            _logger.LogWarning("API.AUTH.LOGIN.FAILED: Failed login attempt for username '{Username}' - Error: {ErrorMessage}", loginDto!.Username, result.ErrorMessage);
        }
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Logs out the currently authenticated user and terminates their session.
    /// </summary>
    /// <returns>
    /// Success response indicating the user has been logged out.
    /// </returns>
    /// <response code="200">User successfully logged out and session terminated</response>
    /// <response code="401">User is not currently authenticated</response>
    /// <response code="500">Internal server error during logout process</response>
    /// <example>
    /// POST /api/auth/logout
    /// </example>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("API.AUTH.LOGOUT: User logout attempt");

        var result = await _authenticationService.LogoutAsync();
        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves the current authenticated user's information including profile details and assigned roles.
    /// </summary>
    /// <returns>
    /// Current user's detailed information including username, email, roles, and profile data.
    /// </returns>
    /// <response code="200">Returns the current user's information successfully</response>
    /// <response code="401">User is not currently authenticated or session has expired</response>
    /// <response code="500">Internal server error retrieving user information</response>
    /// <example>
    /// GET /api/auth/me
    /// </example>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetCurrentUser()
    {
        _logger.LogInformation("API.AUTH.GETUSER: Getting current user information");

        var result = await _authenticationService.GetCurrentUserAsync();
        if (!result.Success)
        {
            _logger.LogWarning("API.AUTH.GETUSER.FAILED: Failed to get current user - Error: {ErrorMessage}", result.ErrorMessage);
        }
        return HandleServiceResult(result);
    }
}
