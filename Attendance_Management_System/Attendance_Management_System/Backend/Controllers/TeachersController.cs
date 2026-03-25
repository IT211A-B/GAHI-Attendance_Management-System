using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing teachers
[Route("api/[controller]")]
public class TeachersController : BaseController
{
    // Service for handling teacher business logic
    private readonly ITeachersService _teachersService;
    private readonly ILogger<TeachersController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public TeachersController(ITeachersService teachersService, ILogger<TeachersController> logger)
    {
        _teachersService = teachersService;
        _logger = logger;
    }

    // Get all teachers - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<TeacherDto>>>> GetAllTeachers()
    {
        var result = await _teachersService.GetAllTeachersAsync();
        return Ok(result);
    }

    // Get all teachers with their assigned sections - Admin only access
    [HttpGet("with-sections")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherListDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<TeacherListDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<TeacherListDto>>>> GetAllTeachersWithSections()
    {
        var result = await _teachersService.GetAllTeachersWithSectionsAsync();
        return Ok(result);
    }

    // Create a new teacher profile for an existing user with teacher role - Admin only
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TeacherDto>>> CreateTeacher([FromBody] CreateTeacherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError<TeacherDto>();
        }

        var result = await _teachersService.CreateTeacherAsync(request);

        if (!result.Success)
        {
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            if (result.Error?.Code == ErrorCodes.AlreadyExists || result.Error?.Code == ErrorCodes.Conflict)
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetTeacherById), new { id = result.Data!.Id }, result);
    }

    // Get a specific teacher by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeacherDto>>> GetTeacherById(int id)
    {
        var result = await _teachersService.GetTeacherByIdAsync(id);

        // Return 404 if teacher not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Update an existing teacher's information
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TeacherDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeacherDto>>> UpdateTeacher(int id, [FromBody] UpdateTeacherRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<TeacherDto>();
        }

        var result = await _teachersService.UpdateTeacherAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the teacher was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Deactivate a teacher (soft delete - marks teacher as inactive)
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateTeacher(int id)
    {
        var result = await _teachersService.DeactivateTeacherAsync(id);

        // Return 404 if teacher not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Activate a teacher (restore inactive teacher to active status)
    [HttpPost("{id}/activate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> ActivateTeacher(int id)
    {
        var result = await _teachersService.ActivateTeacherAsync(id);

        // Return 404 if teacher not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
