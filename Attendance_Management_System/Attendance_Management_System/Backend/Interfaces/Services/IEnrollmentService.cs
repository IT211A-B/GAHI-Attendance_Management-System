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
}
