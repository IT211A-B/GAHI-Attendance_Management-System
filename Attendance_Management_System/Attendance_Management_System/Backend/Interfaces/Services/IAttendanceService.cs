using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.ValueObjects;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Service interface for managing student attendance operations
public interface IAttendanceService
{
    // Marks or corrects attendance for a single student in a specific class session (upsert semantics)
    Task<ApiResponse<AttendanceDto>> MarkAttendanceAsync(MarkAttendanceRequest request, TeacherContext teacherContext);

    // Marks or corrects attendance for multiple students at once (bulk upsert)
    Task<ApiResponse<List<AttendanceDto>>> MarkBulkAttendanceAsync(BulkAttendanceRequest request, TeacherContext teacherContext);

    // Gets attendance summary for all students in a section on a given date
    Task<ApiResponse<AttendanceSummaryDto>> GetSectionAttendanceAsync(int sectionId, DateOnly date, int scheduleId);

    // Gets attendance history for a specific student with optional filters
    Task<ApiResponse<List<AttendanceDto>>> GetStudentAttendanceAsync(int studentId, int? sectionId, DateOnly? from, DateOnly? to);
}
