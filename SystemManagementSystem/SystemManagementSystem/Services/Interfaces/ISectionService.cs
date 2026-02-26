using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Sections;

namespace SystemManagementSystem.Services.Interfaces;

public interface ISectionService
{
    Task<PagedResult<SectionResponse>> GetAllAsync(int page, int pageSize, Guid? programId, Guid? periodId);
    Task<SectionResponse> GetByIdAsync(Guid id);
    Task<SectionResponse> CreateAsync(CreateSectionRequest request);
    Task<SectionResponse> UpdateAsync(Guid id, UpdateSectionRequest request);
    Task DeleteAsync(Guid id);
}
