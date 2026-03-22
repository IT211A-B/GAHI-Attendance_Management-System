using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISectionsService
{
    Task<ApiResponse<List<SectionDto>>> GetAllSectionsAsync();
    Task<ApiResponse<SectionDto>> GetSectionByIdAsync(int id);
    Task<ApiResponse<List<SectionDto>>> GetSectionsByAcademicYearIdAsync(int academicYearId);
    Task<ApiResponse<SectionDto>> CreateSectionAsync(CreateSectionRequest request);
    Task<ApiResponse<SectionDto>> UpdateSectionAsync(int id, UpdateSectionRequest request);
    Task<ApiResponse<bool>> DeleteSectionAsync(int id);
}