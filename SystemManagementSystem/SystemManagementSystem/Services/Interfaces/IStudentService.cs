using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Students;

namespace SystemManagementSystem.Services.Interfaces;

public interface IStudentService
{
    Task<PagedResult<StudentResponse>> GetAllAsync(int page, int pageSize, Guid? sectionId, string? search);
    Task<StudentResponse> GetByIdAsync(Guid id);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request);
    Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest request);
    Task DeleteAsync(Guid id);
    Task<StudentResponse> RegenerateQrCodeAsync(Guid id);
}
