using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Interface for schedule management operations
public interface ISchedulesService
{
    // Get all schedules (teacher sees their assigned sections, admin sees all)
    Task<ApiResponse<List<ScheduleDto>>> GetSchedulesAsync(int userId, string role);

    // Get a single schedule by ID
    Task<ApiResponse<ScheduleDto>> GetScheduleByIdAsync(int id, int userId);

    // Create a new schedule slot with conflict validation
    Task<ApiResponse<ScheduleDto>> CreateScheduleAsync(CreateScheduleRequest request, int userId);

    // Update an existing schedule slot with re-validation
    Task<ApiResponse<ScheduleDto>> UpdateScheduleAsync(int id, UpdateScheduleRequest request, int userId);

    // Delete a schedule slot (owner or admin only)
    Task<ApiResponse<bool>> DeleteScheduleAsync(int id, int userId, bool isAdmin);

    // Get available time slots for a classroom on a specific day
    Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(int classroomId, int dayOfWeek);
}