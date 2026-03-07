using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Staff;

namespace SystemManagementSystem.Services.Interfaces;

public interface IStaffService
{
    Task<PagedResult<StaffResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId, string? search);
    Task<StaffResponse> GetByIdAsync(Guid id);
    Task<StaffResponse> CreateAsync(CreateStaffRequest request);
    Task<StaffResponse> UpdateAsync(Guid id, UpdateStaffRequest request);
    Task DeleteAsync(Guid id);
    Task<StaffResponse> RegenerateQrCodeAsync(Guid id);
}
