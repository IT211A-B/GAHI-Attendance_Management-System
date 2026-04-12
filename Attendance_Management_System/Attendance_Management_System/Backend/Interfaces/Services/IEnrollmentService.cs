using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Service interface for managing student enrollment operations
public interface IEnrollmentService
{
    // Gets a paginated list of enrollments with "pending" status
    Task<EnrollmentListDto> GetPendingEnrollmentsAsync(int? academicYearId, int page, int pageSize);

    // Gets a paginated list of all enrollments with optional filtering by status and academic year
    Task<EnrollmentListDto> GetAllEnrollmentsAsync(string? status, int? academicYearId, int page, int pageSize);

    // Updates the status of an enrollment (approve or reject) - admin only operation
    Task<ApiResponse<EnrollmentDto>> UpdateEnrollmentStatusAsync(int enrollmentId, UpdateEnrollmentStatusRequest request, int adminId);

    // Gets detailed information about a specific enrollment by its ID
    Task<EnrollmentDto?> GetEnrollmentByIdAsync(int enrollmentId);

    // Student self-enrollment - finds matching sections by course and year level, randomly assigns
    Task<ApiResponse<EnrollmentResultDto>> CreateEnrollmentAsync(CreateEnrollmentRequest request, int studentUserId);

    // Admin reassigns student to different section with capacity checks
    Task<ApiResponse<EnrollmentDto>> ReassignSectionAsync(int enrollmentId, ReassignSectionRequest request, int adminId);

    // Returns current enrollment count and capacity status for a section
    Task<SectionCapacityDto?> GetSectionCapacityAsync(int sectionId);

    // Gets sections matching student's course and year level with capacity info
    Task<List<SectionCapacityDto>> GetAvailableSectionsForStudentAsync(int courseId, int yearLevel, int academicYearId);
}
