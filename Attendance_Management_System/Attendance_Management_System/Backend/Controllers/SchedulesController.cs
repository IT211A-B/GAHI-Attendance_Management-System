using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing schedules (CRUD operations with conflict detection)
[Route("api/[controller]")]
public class SchedulesController : BaseController
{
    private readonly ISchedulesService _schedulesService;
    private readonly ILogger<SchedulesController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public SchedulesController(ISchedulesService schedulesService, ILogger<SchedulesController> logger)
    {
        _schedulesService = schedulesService;
        _logger = logger;
    }

    // Get all schedules - Teacher sees their assigned sections, Admin sees all
    [HttpGet]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<ScheduleDto>>>> GetSchedules()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.Unauthorized, "User not authenticated."));
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var result = await _schedulesService.GetSchedulesAsync(currentUserId.Value, userRole);

        return Ok(result);
    }

    // Get a specific schedule by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleDto>>> GetScheduleById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.Unauthorized, "User not authenticated."));
        }

        var result = await _schedulesService.GetScheduleByIdAsync(id, currentUserId.Value);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new schedule slot - Teachers only (must be assigned to section)
    [HttpPost]
    [Authorize(Policy = "TeacherOnly")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ScheduleDto>>> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError<ScheduleDto>();
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.Unauthorized, "User not authenticated."));
        }

        var result = await _schedulesService.CreateScheduleAsync(request, currentUserId.Value);

        if (!result.Success)
        {
            // Handle conflict errors with 409 status
            if (result.Error?.Code == ErrorCodes.ConflictSectionSlot ||
                result.Error?.Code == ErrorCodes.ConflictClassroom ||
                result.Error?.Code == ErrorCodes.ConflictTeacher)
            {
                return Conflict(result);
            }

            // Handle forbidden access
            if (result.Error?.Code == ErrorCodes.Forbidden)
            {
                return StatusCode(403, result);
            }

            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetScheduleById), new { id = result.Data!.Id }, result);
    }

    // Update an existing schedule - Teachers only (must be assigned to section)
    [HttpPut("{id}")]
    [Authorize(Policy = "TeacherOnly")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ScheduleDto>>> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError<ScheduleDto>();
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.Unauthorized, "User not authenticated."));
        }

        var result = await _schedulesService.UpdateScheduleAsync(id, request, currentUserId.Value);

        if (!result.Success)
        {
            // Handle conflict errors with 409 status
            if (result.Error?.Code == ErrorCodes.ConflictSectionSlot ||
                result.Error?.Code == ErrorCodes.ConflictClassroom ||
                result.Error?.Code == ErrorCodes.ConflictTeacher)
            {
                return Conflict(result);
            }

            // Handle not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }

            // Handle forbidden access
            if (result.Error?.Code == ErrorCodes.Forbidden)
            {
                return StatusCode(403, result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a schedule - Teachers (assigned to section) or Admin
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSchedule(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse(ErrorCodes.Unauthorized, "User not authenticated."));
        }

        // Check if user is admin
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var isAdmin = userRole == "admin";

        var result = await _schedulesService.DeleteScheduleAsync(id, currentUserId.Value, isAdmin);

        if (!result.Success)
        {
            // Handle conflict (schedule has attendance records)
            if (result.Error?.Code == ErrorCodes.Conflict)
            {
                return Conflict(result);
            }

            // Handle not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }

            // Handle forbidden access
            if (result.Error?.Code == ErrorCodes.Forbidden)
            {
                return StatusCode(403, result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    // Get available time slots for a classroom on a specific day
    [HttpGet("available")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<AvailableSlotDto>>>> GetAvailableSlots(
        [FromQuery] int classroomId,
        [FromQuery] int dayOfWeek)
    {
        var result = await _schedulesService.GetAvailableSlotsAsync(classroomId, dayOfWeek);

        if (!result.Success)
        {
            // Handle not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }
}