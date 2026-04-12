using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISubjectsService
{
    Task<ApiResponse<List<SubjectDto>>> GetAllSubjectsAsync();
    Task<ApiResponse<SubjectDto>> GetSubjectByIdAsync(int id);
    Task<ApiResponse<List<SubjectDto>>> GetSubjectsByCourseIdAsync(int courseId);
    Task<ApiResponse<SubjectDto>> CreateSubjectAsync(CreateSubjectRequest request);
    Task<ApiResponse<SubjectDto>> UpdateSubjectAsync(int id, UpdateSubjectRequest request);
    Task<ApiResponse<bool>> DeleteSubjectAsync(int id, int? replacementSubjectId = null);
}