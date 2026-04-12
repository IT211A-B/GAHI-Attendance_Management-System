using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Service interface for student profile operations
public interface IStudentsService
{
    // Retrieves full profile for the authenticated student based on their user ID
    Task<ApiResponse<StudentProfileDto>> GetMyProfileAsync(int userId);

    // Returns appropriate profile based on requester's role:
    // - Admin: Full profile
    // - Teacher: Basic profile (if student is in teacher's section)
    // - Student: Basic profile (if viewing another student), Full profile (if viewing self)
    Task<ApiResponse<object>> GetStudentProfileAsync(int studentId, int requesterUserId, string requesterRole);

    // Returns list of basic profiles for students in a section
    // Used by teachers to view students in their assigned sections
    Task<ApiResponse<List<StudentBasicProfileDto>>> GetStudentsBySectionAsync(int sectionId, int requesterUserId, string requesterRole);
}