using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing student attendance
// Requires Admin or Teacher role to access most endpoints
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrTeacher")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly AppDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceService attendanceService,
        AppDbContext context,
        ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _context = context;
        _logger = logger;
    }

    // Marks attendance for a single student
    [HttpPost("mark")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        // Get the current user's teacher context for authorization
        var teacherContext = await GetTeacherContextAsync();
        if (!teacherContext.CanMarkAttendance)
        {
            return Unauthorized(ApiResponse<AttendanceDto>.ErrorResponse("UNAUTHORIZED", "User cannot mark attendance."));
        }

        var result = await _attendanceService.MarkAttendanceAsync(request, teacherContext);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Marks attendance for multiple students in a single request (bulk operation)
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> MarkBulkAttendance([FromBody] BulkAttendanceRequest request)
    {
        // Get the current user's teacher context for authorization
        var teacherContext = await GetTeacherContextAsync();
        if (!teacherContext.CanMarkAttendance)
        {
            return Unauthorized(ApiResponse<List<AttendanceDto>>.ErrorResponse("UNAUTHORIZED", "User cannot mark attendance."));
        }

        var result = await _attendanceService.MarkBulkAttendanceAsync(request, teacherContext);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Gets attendance records for all students in a section on a specific date
    [HttpGet("section/{sectionId}/date/{date}")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AttendanceSummaryDto>>> GetSectionAttendance(
        int sectionId,
        DateOnly date,
        [FromQuery] int scheduleId)
    {
        var result = await _attendanceService.GetSectionAttendanceAsync(sectionId, date, scheduleId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Gets attendance history for a specific student with optional date range filter
    // Students can view their own attendance, while admins and teachers can view any student
    [HttpGet("student/{studentId}")]
    [Authorize(Policy = "AllRoles")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetStudentAttendance(
        int studentId,
        [FromQuery] int? sectionId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        // Students can only view their own attendance
        var currentUserId = GetCurrentUserId();
        var userRole = GetUserRole();

        if (userRole == "student")
        {
            // Verify the student belongs to the current user
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (student == null || student.Id != studentId)
            {
                return Unauthorized(ApiResponse<List<AttendanceDto>>.ErrorResponse("UNAUTHORIZED", "You can only view your own attendance records."));
            }
        }

        var result = await _attendanceService.GetStudentAttendanceAsync(studentId, sectionId, from, to);

        return Ok(result);
    }

    // Builds the TeacherContext containing both UserId and TeacherId for the current user
    // UserId: Used for MarkedBy field (FK to User table)
    // TeacherId: Used for section assignment validation (SectionTeachers.TeacherId)
    private async Task<TeacherContext> GetTeacherContextAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return default;
        }

        var userRole = GetUserRole();

        // Admins can mark attendance without being assigned to a section
        if (userRole == "admin")
        {
            return new TeacherContext
            {
                UserId = userId.Value,
                TeacherId = null,
                IsAdmin = true
            };
        }

        // For teachers, look up their Teacher record to get the TeacherId
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId.Value);

        // Return default if teacher record not found
        if (teacher == null)
        {
            return default;
        }

        return new TeacherContext
        {
            UserId = userId.Value,
            TeacherId = teacher.Id,
            IsAdmin = false
        };
    }

    // Extracts the current user's ID from the JWT token claims
    private int? GetCurrentUserId()
    {
        // Try to find the user ID claim using standard identifier or JWT subject claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    // Gets the current user's role from the JWT token claims
    private string? GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
