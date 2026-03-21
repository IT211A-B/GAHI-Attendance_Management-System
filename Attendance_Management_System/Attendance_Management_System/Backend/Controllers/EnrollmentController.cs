using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing student enrollments
// Requires Admin role to access
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(IEnrollmentService enrollmentService, ILogger<EnrollmentController> logger)
    {
        _enrollmentService = enrollmentService;
        _logger = logger;
    }

    // Gets all pending enrollments with optional academic year filter
    [HttpGet("pending")]
    public async Task<ActionResult<EnrollmentListDto>> GetPendingEnrollments(
        [FromQuery] int? academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _enrollmentService.GetPendingEnrollmentsAsync(academicYearId, page, pageSize);
        return Ok(result);
    }

    // Gets all enrollments with optional filtering by status and academic year
    [HttpGet]
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