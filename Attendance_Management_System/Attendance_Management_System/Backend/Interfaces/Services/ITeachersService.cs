using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Service contract for teacher management operations
public interface ITeachersService
{
    // Get all active teachers (basic info)
    Task<List<TeacherDto>> GetAllTeachersAsync();

    // Resolve the teacher profile linked to an authenticated user account
    Task<TeacherDto> GetTeacherByUserIdAsync(int userId);

    // Get all teachers with their assigned sections
    Task<List<TeacherListDto>> GetAllTeachersWithSectionsAsync();

    // Get a single teacher by ID
    Task<TeacherDto> GetTeacherByIdAsync(int id);

    // Create a new teacher profile for an existing user
    Task<TeacherDto> CreateTeacherAsync(CreateTeacherRequest request);

    // Update teacher information
    Task<TeacherDto> UpdateTeacherAsync(int id, UpdateTeacherRequest request);

    // Soft delete - mark teacher as inactive
    Task DeactivateTeacherAsync(int id);

    // Reactivate a deactivated teacher
    Task ActivateTeacherAsync(int id);
}

