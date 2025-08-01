using Inventory.Api.Common;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Mappers;
using Inventory.Api.Models;
using Inventory.Shared.DTOs.Common;
using Inventory.Shared.DTOs.Employees;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services;

public class EmployeeService(InventoryContext context, ILogger<EmployeeService> logger) : IEmployeeService
{
    private readonly InventoryContext _context = context;
    private readonly ILogger<EmployeeService> _logger = logger;

    #region Query Operations

    public async Task<ServiceResult<PagedResult<EmployeeSummaryDto>>> GetAllEmployeesAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        string? sortBy = "LastName",
        bool sortDescending = false)
    {
        // Validate pagination parameters to ensure they are within acceptable ranges
        if (page < 1)
            return ServiceResult<PagedResult<EmployeeSummaryDto>>.BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return ServiceResult<PagedResult<EmployeeSummaryDto>>.BadRequest("Page size must be between 1 and 100");

        // Start with base query for all employees
        var query = _context.Employees.AsQueryable();

        // Filter by active status if specified
        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        // Apply search term filter across first name, last name and employeecode
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.FirstName.Contains(searchTerm) ||
                                     e.LastName.Contains(searchTerm) ||
                                     (e.EmployeeCode != null && e.EmployeeCode.Contains(searchTerm)));
        }

        // Apply sorting based on the specified field and direction
        query = ApplySorting(query, sortBy, sortDescending);

        // Get total count for pagination metadata before applying pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and project to DTOs for efficient data transfer
        var employees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeSummaryDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                EmployeeCode = e.EmployeeCode,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        // Build paginated result with items and metadata
        var result = new PagedResult<EmployeeSummaryDto>
        {
            Items = employees,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<EmployeeSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<EmployeeDto>> GetEmployeeByIdAsync(int id, bool includeInactive = false)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<EmployeeDto>.BadRequest("Invalid employee ID");

        // Start with base query for employees
        var query = _context.Employees.AsQueryable();

        // Filter out inactive employees unless specifically requested
        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        // Find the employee by ID using filtered query
        var employee = await query.FirstOrDefaultAsync(e => e.Id == id);

        // Return not found if employee doesn't exist or doesn't match criteria
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound("Employee not found");

        // Convert entity to DTO and return success result
        return ServiceResult<EmployeeDto>.Ok(employee.ToDto());
    }

    public async Task<ServiceResult<EmployeeDto>> GetEmployeeByNameAsync(string name, bool includeInactive = false)
    {
        // Validate that the name is not null, empty or whitespace
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<EmployeeDto>.BadRequest("Name cannot be null or empty");

        // Start with base query for employees
        var query = _context.Employees.AsQueryable();

        // Filter out inactive employees unless specifically requested
        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        // Find employee by first or last name using case-insensitive comparison with trimmed input
        var trimmedName = name.Trim();
        var employee = await query
            .FirstOrDefaultAsync(e => e.FirstName.ToLower() == trimmedName.ToLower() ||
                                      e.LastName.ToLower() == trimmedName.ToLower());

        // Return not found if employee doesn't exist or doesn't match criteria
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound("Employee not found");

        // Convert entity to DTO and return success result
        return ServiceResult<EmployeeDto>.Ok(employee.ToDto());
    }

    #endregion

    #region Modification Operations

    public async Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
    {
        // Clean and normalize the input data (trim strings, validate ranges)
        var cleanDto = createEmployeeDto.Clean();

        // Check if an employee with the same first and last name or employee code already exists (case-insensitive)
        var existingEmployee = await _context.Employees
            .FirstOrDefaultAsync(e => (e.FirstName.ToLower() == cleanDto.FirstName.ToLower() &&
                                       e.LastName.ToLower() == cleanDto.LastName.ToLower()) ||
                                      (e.EmployeeCode != null && cleanDto.EmployeeCode != null &&
                                       e.EmployeeCode.ToLower() == cleanDto.EmployeeCode.ToLower()));

        // Return conflict if an employee with the same name or code already exists
        if (existingEmployee != null)
        {
            _logger.LogWarning("EMPLOYEE.CREATE.FAILED: Attempted to create an employee with duplicate name or employee code: {Name}, {Code}",
                cleanDto.FirstName + " " + cleanDto.LastName, cleanDto.EmployeeCode);
            return ServiceResult<EmployeeDto>.Conflict("An employee with the same name or employee code already exists");
        }

        // Convert DTO to entity model and add to database context
        var newEmployee = cleanDto.ToEntity();
        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync();

        // Log successful creation for audit purpose
        _logger.LogInformation("EMPLOYEE.CREATE.SUCCESS: Created new employee: {FullName} with ID {EmployeeId}",
            newEmployee.FullName, newEmployee.Id);

        // Return success result with created employee data and 201 status
        return ServiceResult<EmployeeDto>.Ok(newEmployee.ToDto(), 201); // 201 Created
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<EmployeeDto>.BadRequest("Invalid employee ID");

        // Clean and normalize the input data (trim strings, validate ranges)
        var cleanDto = updateEmployeeDto.Clean();

        // Find the existing employee to update
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound($"Employee with ID {id} not found");

        // Check if an employee with the same first and last name or employee code already exists (case-insensitive)
        var duplicateCheck = await _context.Employees
            .Where(e => e.Id != id) // Exclude current employee from check
            .FirstOrDefaultAsync(e => (e.FirstName.ToLower() == cleanDto.FirstName.ToLower() &&
                                        e.LastName.ToLower() == cleanDto.LastName.ToLower()) ||
                                        (e.EmployeeCode != null && cleanDto.EmployeeCode != null &&
                                         e.EmployeeCode.ToLower() == cleanDto.EmployeeCode.ToLower()));

        // Return conflict if an employee with the same name or code already exists
        if (duplicateCheck != null)
        {
            _logger.LogWarning("EMPLOYEE.UPDATE.FAILED: Attempted to update employee with duplicate name or employee code: {Name}, {Code}",
                cleanDto.FirstName + " " + cleanDto.LastName, cleanDto.EmployeeCode);
            return ServiceResult<EmployeeDto>.Conflict("An employee with the same name or employee code already exists");
        }

        // Apply updates from DTO to existing entity
        employee.UpdateFromDto(cleanDto);
        await _context.SaveChangesAsync();

        // Log successful update for audit purpose
        _logger.LogInformation("EMPLOYEE.UPDATE.SUCCESS: Updated employee: {FullName} with ID {EmployeeId}",
            employee.FullName, employee.Id);

        // Return success result with updated employee data
        return ServiceResult<EmployeeDto>.Ok(employee.ToDto());
    }

    public async Task<ServiceResult> DeleteEmployeeAsync(int id)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult.BadRequest("Invalid employee ID");

        // Find the employee to delete(includes both active and inactive employees)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return ServiceResult.NotFound("Employee not found");

        // Prevent duplicate soft deletion of already inactive employees
        if (!employee.IsActive)
            return ServiceResult.BadRequest("Employee is already deleted");

        // Perform soft delete by marking as inactive and updating timestamp
        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        // Save changes to persist soft deletion
        await _context.SaveChangesAsync();

        // Log successful soft deletion for audit purpose
        _logger.LogInformation("EMPLOYEE.DELETE.SUCCESS: Soft deleted employee: {FullName} with ID {EmployeeId}",
            employee.FullName, employee.Id);

        // Return success result with 204 No Content status
        return ServiceResult.Ok(204); // 204 No Content
    }


    public async Task<ServiceResult<EmployeeDto>> RestoreEmployeeAsync(int id)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<EmployeeDto>.BadRequest("Invalid employee ID");

        // Find the employee to restore (includes both active and inactive employees)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound($"Employee with ID {id} not found");

        // Prevent restoration of already active employees
        if (employee.IsActive)
            return ServiceResult<EmployeeDto>.BadRequest("Employee is already active");

        // Restore the employee by marking as active and updating timestamp
        employee.IsActive = true;
        employee.UpdatedAt = DateTime.UtcNow;

        // Save changes to persist the restoration
        await _context.SaveChangesAsync();

        // Log successful restoration for audit purpose
        _logger.LogInformation("EMPLOYEE.RESTORE.SUCCESS: Restored employee: {FullName} with ID {EmployeeId}",
            employee.FullName, employee.Id);

        // Return success result with updated employee data
        return ServiceResult<EmployeeDto>.Ok(employee.ToDto());
    }

    #endregion

    #region Private Helper Methods

    private static IQueryable<Employee> ApplySorting(IQueryable<Employee> query, string? sortBy, bool sortDescending)
    {
        // Clean and normalize the sort field name
        var validSortBy = sortBy?.Trim();

        var orderedQuery = validSortBy?.ToLower() switch
        {
            "firstname" => sortDescending ? query.OrderByDescending(e => e.FirstName) : query.OrderBy(e => e.FirstName),
            "lastname" => sortDescending ? query.OrderByDescending(e => e.LastName) : query.OrderBy(e => e.LastName),
            "position" => sortDescending ? query.OrderByDescending(e => e.Position) : query.OrderBy(e => e.Position),
            "department" => sortDescending ? query.OrderByDescending(e => e.Department) : query.OrderBy(e => e.Department),
            _ => sortDescending ? query.OrderByDescending(e => e.LastName) : query.OrderBy(e => e.LastName)
        };

        return orderedQuery;
    }

    #endregion
}
