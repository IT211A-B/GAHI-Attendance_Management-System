using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing student enrollments
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(IEnrollmentService enrollmentService, ILogger<EnrollmentController> logger)
    {
        _enrollmentService = enrollmentService;
        _logger = logger;
    }

    // Student self-enrollment - creates enrollment request for matching section
    [HttpPost]
    [Authorize(Policy = "StudentOnly")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResultDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EnrollmentResultDto>>> CreateEnrollment([FromBody] CreateEnrollmentRequest request)
    {
        var studentUserId = GetCurrentUserId();
        if (studentUserId == null)
        {
            return Unauthorized(ApiResponse<EnrollmentResultDto>.ErrorResponse("UNAUTHORIZED", "Invalid token."));
        }

        var result = await _enrollmentService.CreateEnrollmentAsync(request, studentUserId.Value);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Gets all pending enrollments with optional academic year filter - Admin only
    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnrollmentListDto>> GetPendingEnrollments(
        [FromQuery] int? academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _enrollmentService.GetPendingEnrollmentsAsync(academicYearId, page, pageSize);
        return Ok(result);
    }

    // Gets all enrollments with optional filtering by status and academic year - Admin only
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(EnrollmentListDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnrollmentListDto>> GetAllEnrollments(
        [FromQuery] string? status,
        [FromQuery] int? academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _enrollmentService.GetAllEnrollmentsAsync(status, academicYearId, page, pageSize);
        return Ok(result);
    }

    // Gets a specific enrollment by its ID
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> GetEnrollment(int id)
    {
        var result = await _enrollmentService.GetEnrollmentByIdAsync(id);

        if (result == null)
        {
            return NotFound(ApiResponse<EnrollmentDto>.ErrorResponse("NOT_FOUND", "Enrollment not found."));
        }

        return Ok(ApiResponse<EnrollmentDto>.SuccessResponse(result));
    }

    // Updates enrollment status (approve or reject) - admin only operation
    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> UpdateEnrollmentStatus(
        int id,
        [FromBody] UpdateEnrollmentStatusRequest request)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<EnrollmentDto>.ErrorResponse("UNAUTHORIZED", "Invalid token."));
        }

        var result = await _enrollmentService.UpdateEnrollmentStatusAsync(id, request, adminId.Value);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Admin reassigns student to different section
    [HttpPut("{id}/reassign")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> ReassignSection(
        int id,
        [FromBody] ReassignSectionRequest request)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<EnrollmentDto>.ErrorResponse("UNAUTHORIZED", "Invalid token."));
        }

        var result = await _enrollmentService.ReassignSectionAsync(id, request, adminId.Value);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Gets capacity information for a specific section - Admin/Teacher only
    [HttpGet("capacity/{sectionId}")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(SectionCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SectionCapacityDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SectionCapacityDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(SectionCapacityDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionCapacityDto>> GetSectionCapacity(int sectionId)
    {
        var result = await _enrollmentService.GetSectionCapacityAsync(sectionId);

        if (result == null)
        {
            return NotFound(ApiResponse<SectionCapacityDto>.ErrorResponse("NOT_FOUND", "Section not found."));
        }

        return Ok(result);
    }

    // Gets available sections for student based on course and year level
    [HttpGet("available-sections")]
    [Authorize(Policy = "StudentOnly")]
    [ProducesResponseType(typeof(List<SectionCapacityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<SectionCapacityDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(List<SectionCapacityDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<SectionCapacityDto>>> GetAvailableSections(
        [FromQuery] int courseId,
        [FromQuery] int yearLevel,
        [FromQuery] int academicYearId)
    {
        var result = await _enrollmentService.GetAvailableSectionsForStudentAsync(courseId, yearLevel, academicYearId);
        return Ok(result);
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
}