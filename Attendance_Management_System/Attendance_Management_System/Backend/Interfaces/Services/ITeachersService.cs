using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Service contract for teacher management operations
public interface ITeachersService
{
    // Get all active teachers (basic info)
    Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync();

    // Get all teachers with their assigned sections
    Task<ApiResponse<List<TeacherListDto>>> GetAllTeachersWithSectionsAsync();

    // Get a single teacher by ID
    Task<ApiResponse<TeacherDto>> GetTeacherByIdAsync(int id);

    // Create a new teacher profile for an existing user
    Task<ApiResponse<TeacherDto>> CreateTeacherAsync(CreateTeacherRequest request);

    // Update teacher information
    Task<ApiResponse<TeacherDto>> UpdateTeacherAsync(int id, UpdateTeacherRequest request);

    // Soft delete - mark teacher as inactive
    Task<ApiResponse<bool>> DeactivateTeacherAsync(int id);

    // Reactivate a deactivated teacher
    Task<ApiResponse<bool>> ActivateTeacherAsync(int id);
}
