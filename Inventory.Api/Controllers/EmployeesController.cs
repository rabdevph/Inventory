using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Controllers;
using Inventory.Api.Interfaces;
using Inventory.Api.Common;
using Inventory.Shared.DTOs.Employees;
using Inventory.Shared.DTOs.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.Api.Controllers;

/// <summary>
/// API Controller for managing employees.
/// Provides CRUD operations for employees including creation, retrieval, updating, and soft deletion.
/// Supports advanced features like filtering, pagination, sorting, and employee restoration.
/// </summary>
/// <remarks>
/// This controller handles all employee-related operations in the inventory management system.
/// All operations return standardized responses using the ServiceResult pattern for consistent error handling.
/// 
/// Key Features:
/// - Full CRUD operations (Create, Read, Update, Delete)
/// - Soft deletion with restoration capability
/// - Advanced filtering and search functionality
/// - Pagination support for large datasets
/// - Comprehensive error handling and logging
/// - OpenAPI/Swagger documentation
/// </remarks>
/// <remarks>
/// Initializes a new instance of the EmployeesController.
/// </remarks>
/// <param name="employeeService">The employee service for business logic operations</param>
/// <param name="logger">Logger instance for this controller</param>
/// <exception cref="ArgumentNullException">Thrown when employeeService or logger is null</exception>
[ApiController]
[Route("api/employees")]
[Authorize(Policy = "CanManageEmployees")]
[Produces("application/json")]
[Tags("Employees")]
public class EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger) : ApiBaseController
{
    private readonly IEmployeeService _employeeService = employeeService;
    private readonly ILogger<EmployeesController> _logger = logger;

    /// <summary>
    /// Retrieves all employees with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of employees per page (default: 20, range: 1-100)</param>
    /// <param name="isActive">Filter by active status. null = all employees, true = active only, false = inactive only</param>
    /// <param name="searchTerm">Search term to filter by employee name or employee code (case-insensitive)</param>
    /// <param name="sortBy">Field to sort by. Valid values: "FirstName", "LastName", "Position", "Department" (default: "LastName")</param>
    /// <param name="sortDescending">Sort direction. true = descending, false = ascending (default: false)</param>
    /// <returns>
    /// A paginated list of employee summaries matching the specified criteria.
    /// </returns>
    /// <response code="200">Returns the paginated list of employees</response>
    /// <response code="400">Invalid parameters provided (e.g., invalid page number, page size out of range)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/employees?page=1&amp;pageSize=10&amp;isActive=true&amp;searchTerm=john&amp;sortBy=LastName&amp;sortDescending=false
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = "LastName",
        [FromQuery] bool sortDescending = false)
    {
        _logger.LogInformation("API.EMPLOYEES.GET: Getting all employees - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            page, pageSize, searchTerm);

        var result = await _employeeService.GetAllEmployeesAsync(
            page, pageSize, isActive, searchTerm, sortBy, sortDescending);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.GET.FAILED: Failed to retrieve employees - Error: {ErrorMessage}", result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific employee by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to retrieve (must be positive integer)</param>
    /// <param name="includeInactive">
    /// Whether to include inactive (soft-deleted) employees in the search.
    /// Default is false (active employees only).
    /// </param>
    /// <returns>
    /// The requested employee details if found, otherwise a 404 Not Found response.
    /// </returns>
    /// <response code="200">Returns the requested employee details</response>
    /// <response code="400">Invalid ID parameter (must be positive integer)</response>
    /// <response code="404">Employee with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/employees/123
    /// GET /api/employees/123?includeInactive=true
    /// </example>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetEmployeeById(int id, [FromQuery] bool includeInactive = false)
    {
        _logger.LogInformation("API.EMPLOYEES.GETBYID: Getting employee by ID: {EmployeeId}, IncludeInactive: {IncludeInactive}", id, includeInactive);

        var result = await _employeeService.GetEmployeeByIdAsync(id, includeInactive);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.GETBYID.FAILED: Failed to retrieve employee by ID {EmployeeId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific employee by their name.
    /// </summary>
    /// <param name="name">
    /// The name of the employee to retrieve. Search is case-insensitive and matches first or last name.
    /// Cannot be null, empty, or whitespace only.
    /// </param>
    /// <param name="includeInactive">
    /// Whether to include inactive (soft-deleted) employees in the search.
    /// Default is false (active employees only).
    /// </param>
    /// <returns>
    /// The requested employee details if found, otherwise a 404 Not Found response.
    /// </returns>
    /// <response code="200">Returns the requested employee details</response>
    /// <response code="400">Employee name is null, empty, or contains only whitespace</response>
    /// <response code="404">Employee with the specified name was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/employees/by-name/John
    /// GET /api/employees/by-name/Smith?includeInactive=true
    /// </example>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(EmployeeDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetEmployeeByName(string name, [FromQuery] bool includeInactive = false)
    {
        _logger.LogInformation("API.EMPLOYEES.GETBYNAME: Getting employee by name: {EmployeeName}, IncludeInactive: {IncludeInactive}", name, includeInactive);

        var result = await _employeeService.GetEmployeeByNameAsync(name, includeInactive);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.GETBYNAME.FAILED: Failed to retrieve employee by name '{EmployeeName}' - Error: {ErrorMessage}", name, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Creates a new employee in the system.
    /// </summary>
    /// <param name="createEmployeeDto">
    /// The employee data for creation. All required fields must be provided and valid.
    /// Employee names and codes must be unique across the system.
    /// </param>
    /// <returns>
    /// The newly created employee with assigned ID and system-generated fields.
    /// </returns>
    /// <response code="201">Employee was successfully created</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="409">An employee with the specified name or code already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/employees
    ///     {
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "position": "Software Engineer",
    ///         "department": "IT",
    ///         "employeeCode": "EMP001"
    ///     }
    /// 
    /// Business Rules:
    /// - Employee names (first + last) must be unique (case-insensitive)
    /// - Employee codes must be unique (case-insensitive)
    /// - First and last names are required and cannot exceed 100 characters
    /// - Position and department are optional but cannot exceed 200 characters
    /// - Employee code is optional but cannot exceed 50 characters
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto createEmployeeDto)
    {
        _logger.LogInformation("API.EMPLOYEES.CREATE: Creating new employee: {EmployeeName}",
            createEmployeeDto != null ? $"{createEmployeeDto.FirstName} {createEmployeeDto.LastName}" : "null");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.EMPLOYEES.CREATE.VALIDATION: Model validation failed for employee creation - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _employeeService.CreateEmployeeAsync(createEmployeeDto!);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.CREATE.FAILED: Failed to create employee '{EmployeeName}' - Error: {ErrorMessage}",
                $"{createEmployeeDto!.FirstName} {createEmployeeDto.LastName}", result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Updates an existing employee with new information.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to update (must be positive integer)</param>
    /// <param name="updateEmployeeDto">
    /// The updated employee data. Only provided fields will be modified.
    /// Employee names and codes must remain unique across the system.
    /// </param>
    /// <returns>
    /// The updated employee with all current information including modified timestamps.
    /// </returns>
    /// <response code="200">Employee was successfully updated</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="404">Employee with the specified ID was not found</response>
    /// <response code="409">Another employee with the specified name or code already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/employees/123
    ///     {
    ///         "firstName": "John",
    ///         "lastName": "Smith",
    ///         "position": "Senior Software Engineer",
    ///         "department": "IT",
    ///         "employeeCode": "EMP001"
    ///     }
    /// 
    /// Business Rules:
    /// - Employee must exist and be accessible
    /// - Employee names (first + last) must remain unique (case-insensitive) excluding the current employee
    /// - Employee codes must remain unique (case-insensitive) excluding the current employee
    /// - First and last names are required and cannot exceed 100 characters
    /// - Position and department are optional but cannot exceed 200 characters
    /// - Employee code is optional but cannot exceed 50 characters
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
    {
        _logger.LogInformation("API.EMPLOYEES.UPDATE: Updating employee ID: {EmployeeId} with name: {EmployeeName}",
            id, updateEmployeeDto != null ? $"{updateEmployeeDto.FirstName} {updateEmployeeDto.LastName}" : "null");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.EMPLOYEES.UPDATE.VALIDATION: Model validation failed for employee update ID: {EmployeeId} - Errors: {ValidationErrors}", id, validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto!);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.UPDATE.FAILED: Failed to update employee ID: {EmployeeId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Deactivates an employee by marking them as inactive while preserving all data.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to deactivate (must be positive integer)</param>
    /// <returns>
    /// No content on successful deactivation. The employee is marked as inactive but data is preserved.
    /// </returns>
    /// <response code="204">Employee was successfully deactivated (marked as inactive)</response>
    /// <response code="400">Invalid ID parameter or employee is already inactive</response>
    /// <response code="404">Employee with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation performs a "soft deactivation" which means:
    /// - The employee is marked as inactive (IsActive = false)
    /// - All employee data is preserved in the database
    /// - The employee will not appear in normal queries (unless specifically requested)
    /// - The employee can be reactivated using the activate endpoint
    /// - Historical data and relationships are maintained
    /// 
    /// Use the activate endpoint (PATCH /api/employees/{id}/activate) to reactivate the employee.
    /// </remarks>
    [HttpPatch("{id:int}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeactivateEmployee(int id)
    {
        _logger.LogInformation("API.EMPLOYEES.DEACTIVATE: Deactivating employee ID: {EmployeeId}", id);

        var result = await _employeeService.DeleteEmployeeAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.DEACTIVATE.FAILED: Failed to deactivate employee ID: {EmployeeId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Activates a previously deactivated employee, making them active again.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to activate (must be positive integer)</param>
    /// <returns>
    /// The activated employee with updated status and timestamps.
    /// </returns>
    /// <response code="200">Employee was successfully activated and is now active</response>
    /// <response code="400">Invalid ID parameter or employee is already active</response>
    /// <response code="404">Employee with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation activates a deactivated employee by:
    /// - Setting IsActive to true
    /// - Updating the UpdatedAt timestamp
    /// - Making the employee visible in normal queries again
    /// - Preserving all original employee data and history
    /// 
    /// This endpoint can activate employees that were deactivated using the deactivate endpoint.
    /// If an employee was never deactivated or doesn't exist, appropriate error responses will be returned.
    /// 
    /// After activation, the employee will behave exactly as they did before deactivation,
    /// maintaining all their properties, relationships, and transaction history.
    /// </remarks>
    [HttpPatch("{id:int}/activate")]
    [ProducesResponseType(typeof(EmployeeDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> ActivateEmployee(int id)
    {
        _logger.LogInformation("API.EMPLOYEES.ACTIVATE: Activating employee ID: {EmployeeId}", id);

        var result = await _employeeService.RestoreEmployeeAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.EMPLOYEES.ACTIVATE.FAILED: Failed to activate employee ID: {EmployeeId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }
}
