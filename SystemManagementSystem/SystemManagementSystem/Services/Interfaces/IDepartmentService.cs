using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Departments;

namespace SystemManagementSystem.Services.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentResponse>> GetAllAsync(int page, int pageSize);
    Task<DepartmentResponse> GetByIdAsync(Guid id);
    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request);
    Task<DepartmentResponse> UpdateAsync(Guid id, UpdateDepartmentRequest request);
    Task DeleteAsync(Guid id);
}
