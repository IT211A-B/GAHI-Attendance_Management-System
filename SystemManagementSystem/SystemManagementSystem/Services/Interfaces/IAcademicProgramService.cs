using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPrograms;

namespace SystemManagementSystem.Services.Interfaces;

public interface IAcademicProgramService
{
    Task<PagedResult<AcademicProgramResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId);
    Task<AcademicProgramResponse> GetByIdAsync(Guid id);
    Task<AcademicProgramResponse> CreateAsync(CreateAcademicProgramRequest request);
    Task<AcademicProgramResponse> UpdateAsync(Guid id, UpdateAcademicProgramRequest request);
    Task DeleteAsync(Guid id);
}
