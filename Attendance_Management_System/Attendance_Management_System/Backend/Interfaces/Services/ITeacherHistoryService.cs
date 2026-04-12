using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Interface for teacher history service
// Provides read-only access to teacher's schedules and attendance history
public interface ITeacherHistoryService
{
    // Get all schedule slots for sections the teacher is assigned to
    Task<ApiResponse<List<TeacherScheduleDto>>> GetTeacherSchedulesAsync(int userId);

    // Get attendance history for a specific schedule with optional date filter
    Task<ApiResponse<ScheduleHistoryDto>> GetScheduleHistoryAsync(int scheduleId, int userId, DateOnly? date);
}