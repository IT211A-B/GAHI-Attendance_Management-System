using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAcademicYearsService
{
    Task<List<AcademicYearDto>> GetAllAcademicYearsAsync();
    Task<AcademicYearDto> GetAcademicYearByIdAsync(int id);
    Task<AcademicYearDto> CreateAcademicYearAsync(CreateAcademicYearRequest request);
    Task<AcademicYearDto> UpdateAcademicYearAsync(int id, UpdateAcademicYearRequest request);
    Task DeleteAcademicYearAsync(int id);
    Task<AcademicYearDto> ActivateAcademicYearAsync(int id);
}