using Inventory.Api.Models;
using Inventory.Shared.DTOs.Employees;
using Microsoft.AspNetCore.StaticAssets;

namespace Inventory.Api.Mappers;

// Static class containing extension methods for mapping between Employee entities and DTOs
public static class EmployeeMapper
{
    // Converts a full Employee entity to EmployeeDto (complete employee details)
    public static EmployeeDto ToDto(this Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FullName,
            Position = employee.Position,
            Department = employee.Department,
            EmployeeCode = employee.EmployeeCode,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    // Converts Employee entity to EmployeeSummaryDto (lightweight version for lists/grids)
    public static EmployeeSummaryDto ToSummaryDto(this Employee employee)
    {
        return new EmployeeSummaryDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Position = employee.Position,
            Department = employee.Department,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt
        };
    }

    // Converts a collection of Employees to a collection of EmployeeDtos
    public static IEnumerable<EmployeeDto> ToDto(this IEnumerable<Employee> employees)
    {
        return employees.Select(ToDto);
    }

    // Converts a collection of Employees to a collection of EmployeeSummaryDtos
    public static IEnumerable<EmployeeSummaryDto> ToSummaryDto(this IEnumerable<Employee> employees)
    {
        return employees.Select(ToSummaryDto);
    }

    // Creates a new Employee entity from CreateEmployeeDto (for POST operations)
    public static Employee ToEntity(this CreateEmployeeDto createDto)
    {
        return new Employee
        {
            FirstName = createDto.FirstName.Trim(),
            LastName = createDto.LastName.Trim(),
            Position = createDto.Position?.Trim(),
            Department = createDto.Department?.Trim(),
            EmployeeCode = createDto.EmployeeCode?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Updates an existing Employee entity from UpdateEmployeeDto (for PUT operations)
    public static Employee UpdateFromDto(this Employee employee, UpdateEmployeeDto updateDto)
    {
        employee.FirstName = updateDto.FirstName.Trim();
        employee.LastName = updateDto.LastName.Trim();
        employee.Position = updateDto.Position?.Trim();
        employee.Department = updateDto.Department?.Trim();
        employee.EmployeeCode = updateDto.EmployeeCode?.Trim();
        employee.UpdatedAt = DateTime.UtcNow;

        return employee;
    }

    // Cleans up CreateEmployeeDto by trimming strings and handling nulls
    public static CreateEmployeeDto Clean(this CreateEmployeeDto createDto)
    {
        return new CreateEmployeeDto
        {
            FirstName = createDto.FirstName.Trim(),
            LastName = createDto.LastName.Trim(),
            Position = string.IsNullOrWhiteSpace(createDto.Position)
                ? null
                : createDto.Position.Trim(),
            Department = string.IsNullOrWhiteSpace(createDto.Department)
                ? null
                : createDto.Department.Trim(),
            EmployeeCode = string.IsNullOrWhiteSpace(createDto.EmployeeCode)
                ? null
                : createDto.EmployeeCode.Trim()
        };
    }

    // Cleans up UpdateEmployeeDto by trimming strings and handling nulls
    public static UpdateEmployeeDto Clean(this UpdateEmployeeDto updateDto)
    {
        return new UpdateEmployeeDto
        {
            FirstName = updateDto.FirstName.Trim(),
            LastName = updateDto.LastName.Trim(),
            Position = string.IsNullOrWhiteSpace(updateDto.Position)
                ? null
                : updateDto.Position.Trim(),
            Department = string.IsNullOrWhiteSpace(updateDto.Department)
                ? null
                : updateDto.Department.Trim(),
            EmployeeCode = string.IsNullOrWhiteSpace(updateDto.EmployeeCode)
                ? null
                : updateDto.EmployeeCode.Trim()
        };
    }
}
