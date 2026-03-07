using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPeriods;

namespace SystemManagementSystem.Services.Interfaces;

public interface IAcademicPeriodService
{
    Task<PagedResult<AcademicPeriodResponse>> GetAllAsync(int page, int pageSize);
    Task<AcademicPeriodResponse> GetByIdAsync(Guid id);
    Task<AcademicPeriodResponse> GetCurrentAsync();
    Task<AcademicPeriodResponse> CreateAsync(CreateAcademicPeriodRequest request);
    Task<AcademicPeriodResponse> UpdateAsync(Guid id, UpdateAcademicPeriodRequest request);
    Task<AcademicPeriodResponse> SetCurrentAsync(Guid id);
    Task DeleteAsync(Guid id);
}
