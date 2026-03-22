using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for teacher history endpoints
// Provides read-only access to teacher's schedules and attendance history
[ApiController]
[Route("api/teacher")]
[Authorize(Policy = "TeacherOnly")]
public class TeacherHistoryController : ControllerBase
{
    private readonly ITeacherHistoryService _teacherHistoryService;
    private readonly ILogger<TeacherHistoryController> _logger;

    public TeacherHistoryController(
        ITeacherHistoryService teacherHistoryService,
        ILogger<TeacherHistoryController> logger)
    {
        _teacherHistoryService = teacherHistoryService;
        _logger = logger;
    }

    // GET: api/teacher/schedules
    // Returns all schedule slots for sections the authenticated teacher is assigned to
    [HttpGet("schedules")]
    public async Task<ActionResult<ApiResponse<List<TeacherScheduleDto>>>> GetSchedules()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<TeacherScheduleDto>>.ErrorResponse(
                "UNAUTHORIZED",
                "User not authenticated."));
        }

        var result = await _teacherHistoryService.GetTeacherSchedulesAsync(userId.Value);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // GET: api/teacher/schedules/{id}/history
    // Returns attendance history for a specific schedule with optional date filter
    [HttpGet("schedules/{id}/history")]
    public async Task<ActionResult<ApiResponse<ScheduleHistoryDto>>> GetScheduleHistory(
        int id,
        [FromQuery] DateOnly? date)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<ScheduleHistoryDto>.ErrorResponse(
                "UNAUTHORIZED",
                "User not authenticated."));
        }

        var result = await _teacherHistoryService.GetScheduleHistoryAsync(id, userId.Value, date);

        if (!result.Success)
        {
            if (result.Error?.Code == "FORBIDDEN")
            {
                return Forbid();
            }
            return NotFound(result);
        }

        return Ok(result);
    }

    // Extracts the current user's ID from the JWT token claims
    private int? GetCurrentUserId()
    {
        // Try to find the user ID claim using standard identifier or JWT subject claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}