using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAcademicYearsService
{
    Task<ApiResponse<List<AcademicYearDto>>> GetAllAcademicYearsAsync();
    Task<ApiResponse<AcademicYearDto>> GetAcademicYearByIdAsync(int id);
    Task<ApiResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearRequest request);
    Task<ApiResponse<AcademicYearDto>> UpdateAcademicYearAsync(int id, UpdateAcademicYearRequest request);
    Task<ApiResponse<bool>> DeleteAcademicYearAsync(int id);
    Task<ApiResponse<AcademicYearDto>> ActivateAcademicYearAsync(int id);
}