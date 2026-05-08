using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISectionsService
{
    Task<List<SectionDto>> GetAllSectionsAsync();
    Task<List<SectionDto>> GetSectionsByTeacherUserIdAsync(int teacherUserId);
    Task<SectionDto> GetSectionByIdAsync(int id);
    Task<List<SectionDto>> GetSectionsByAcademicYearIdAsync(int academicYearId);
    Task<SectionDto> CreateSectionAsync(CreateSectionRequest request);
    Task<SectionDto> UpdateSectionAsync(int id, UpdateSectionRequest request);
    Task DeleteSectionAsync(int id);

    // Teacher assignment and timetable methods
    Task<TimetableResponse> GetTimetableAsync(int sectionId, int? currentUserId = null);
    Task<List<SectionTeacherDto>> GetSectionTeachersAsync(int sectionId);
    Task<SectionTeacherDto> AssignTeacherToSectionAsync(int sectionId, AssignTeacherRequest request);
    Task RemoveTeacherFromSectionAsync(int sectionId, int teacherId, bool isAdmin = false, bool removeOwnedSchedules = false);

    // Filter sections by course and year level for enrollment
    Task<List<SectionDto>> GetSectionsByCourseAndYearLevelAsync(int courseId, int yearLevel, int? academicYearId = null);
}

