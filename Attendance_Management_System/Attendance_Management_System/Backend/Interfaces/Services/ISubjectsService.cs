using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISubjectsService
{
    Task<List<SubjectDto>> GetAllSubjectsAsync();
    Task<SubjectDto> GetSubjectByIdAsync(int id);
    Task<List<SubjectDto>> GetSubjectsByCourseIdAsync(int courseId);
    Task<SubjectDto> CreateSubjectAsync(CreateSubjectRequest request);
    Task<SubjectDto> UpdateSubjectAsync(int id, UpdateSubjectRequest request);
    Task DeleteSubjectAsync(int id, int? replacementSubjectId = null);
}
