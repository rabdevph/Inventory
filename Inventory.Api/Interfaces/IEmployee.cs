using Inventory.Api.Common;
using Inventory.Api.Models;
using Inventory.Shared.DTOs.Common;
using Inventory.Shared.DTOs.Employees;

namespace Inventory.Api.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<PagedResult<EmployeeSummaryDto>>> GetAllEmployeesAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        string? sortBy = "LastName",
        bool sortDescending = false
    );

    Task<ServiceResult<EmployeeDto>> GetEmployeeByIdAsync(int id, bool includeInactive = false);
    Task<ServiceResult<EmployeeDto>> GetEmployeeByNameAsync(string name, bool includeInactive = false);
    Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto);
    Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto);
    Task<ServiceResult> DeleteEmployeeAsync(int id);
    Task<ServiceResult<EmployeeDto>> RestoreEmployeeAsync(int id);
}
