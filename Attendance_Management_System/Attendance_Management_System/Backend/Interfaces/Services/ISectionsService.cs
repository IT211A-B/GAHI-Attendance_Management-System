using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISectionsService
{
    Task<List<SectionDto>> GetAllSectionsAsync(CancellationToken cancellationToken = default);
    Task<List<SectionDto>> GetSectionsByTeacherUserIdAsync(int teacherUserId, CancellationToken cancellationToken = default);
    Task<SectionDto> GetSectionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SectionDto>> GetSectionsByAcademicYearIdAsync(int academicYearId, CancellationToken cancellationToken = default);
    Task<SectionDto> CreateSectionAsync(CreateSectionRequest request, CancellationToken cancellationToken = default);
    Task<SectionDto> UpdateSectionAsync(int id, UpdateSectionRequest request, CancellationToken cancellationToken = default);
    Task DeleteSectionAsync(int id, CancellationToken cancellationToken = default);

    // Teacher assignment and timetable methods
    Task<TimetableResponse> GetTimetableAsync(int sectionId, int? currentUserId = null, CancellationToken cancellationToken = default);
    Task<List<SectionTeacherDto>> GetSectionTeachersAsync(int sectionId, CancellationToken cancellationToken = default);
    Task<SectionTeacherDto> AssignTeacherToSectionAsync(int sectionId, AssignTeacherRequest request, CancellationToken cancellationToken = default);
    Task RemoveTeacherFromSectionAsync(int sectionId, int teacherId, bool isAdmin = false, bool removeOwnedSchedules = false, CancellationToken cancellationToken = default);

    // Filter sections by course and year level for enrollment
    Task<List<SectionDto>> GetSectionsByCourseAndYearLevelAsync(int courseId, int yearLevel, int? academicYearId = null, CancellationToken cancellationToken = default);
}

