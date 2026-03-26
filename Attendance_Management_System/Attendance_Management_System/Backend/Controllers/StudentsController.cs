using System.Security.Claims;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for student profile operations with role-based access control
[Route("api/[controller]")]
public class StudentsController : BaseController
{
    private readonly IStudentsService _studentsService;
    private readonly ILogger<StudentsController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public StudentsController(IStudentsService studentsService, ILogger<StudentsController> logger)
    {
        _studentsService = studentsService;
        _logger = logger;
    }

    // Get the authenticated student's own profile (students only)
    [HttpGet("profile")]
    [Authorize(Policy = "StudentOnly")]
    [ProducesResponseType(typeof(ApiResponse<StudentProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<StudentProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StudentProfileDto>>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<StudentProfileDto>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to identify user."));
        }

        var result = await _studentsService.GetMyProfileAsync(userId.Value);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Get a specific student by ID (role-based data filtering applied)
    [HttpGet("{id}")]
    [Authorize(Policy = "AllRoles")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetStudentById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to identify user."));
        }

        // Get the requester's role from claims
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        var result = await _studentsService.GetStudentProfileAsync(id, userId.Value, role);

        if (!result.Success)
        {
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            if (result.Error?.Code == ErrorCodes.Forbidden)
            {
                return StatusCode(403, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Get all students in a section (admin and teachers only)
    [HttpGet("section/{sectionId}")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentBasicProfileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentBasicProfileDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentBasicProfileDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentBasicProfileDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentBasicProfileDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<StudentBasicProfileDto>>>> GetStudentsBySection(int sectionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<StudentBasicProfileDto>>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to identify user."));
        }

        // Get the requester's role from claims
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        var result = await _studentsService.GetStudentsBySectionAsync(sectionId, userId.Value, role);

        if (!result.Success)
        {
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            if (result.Error?.Code == ErrorCodes.Forbidden)
            {
                return StatusCode(403, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}