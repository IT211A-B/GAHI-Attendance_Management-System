using Attendance_Management_System.Backend.ValueObjects;
using Attendance_Management_System.Backend.ViewModels.Sections;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ISectionPageService
{
    Task<SectionManagementIndexViewModel> BuildSectionManagementIndexViewModelAsync(int currentUserId, string role);
    Task<TimetableIndexViewModel> BuildTimetableIndexViewModelAsync(int currentUserId, string role, int? requestedSectionId);
    Task<SectionAttendanceIndexViewModel> BuildSectionAttendanceIndexViewModelAsync(
        int currentUserId,
        string role,
        int? requestedSectionId,
        int? requestedScheduleId,
        DateOnly? requestedAttendanceDate);

    Task<(bool Success, TeacherContext Context, string? Error)> BuildTeacherContextAsync(int userId, string role);
    Task<(bool IsValid, string ErrorMessage)> ValidateSubjectSelectionForSectionAsync(int sectionId, int subjectId);
}
