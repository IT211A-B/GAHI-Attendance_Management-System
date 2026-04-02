using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing classrooms (CRUD operations)
[Route("api/[controller]")]
public class ClassroomsController : BaseController
{
    // Service for handling classroom business logic
    private readonly IClassroomsService _classroomsService;
    private readonly ILogger<ClassroomsController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public ClassroomsController(IClassroomsService classroomsService, ILogger<ClassroomsController> logger)
    {
        _classroomsService = classroomsService;
        _logger = logger;
    }

    // Get all classrooms - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<ClassroomDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ClassroomDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ClassroomDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<ClassroomDto>>>> GetAllClassrooms()
    {
        var result = await _classroomsService.GetAllClassroomsAsync();
        return Ok(result);
    }

    // Get a specific classroom by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClassroomDto>>> GetClassroomById(int id)
    {
        var result = await _classroomsService.GetClassroomByIdAsync(id);

        // Return 404 if classroom not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new classroom
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ClassroomDto>>> CreateClassroom([FromBody] CreateClassroomRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<ClassroomDto>();
        }

        var result = await _classroomsService.CreateClassroomAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetClassroomById), new { id = result.Data!.Id }, result);
    }

    // Update an existing classroom
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ClassroomDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClassroomDto>>> UpdateClassroom(int id, [FromBody] UpdateClassroomRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<ClassroomDto>();
        }

        var result = await _classroomsService.UpdateClassroomAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the classroom was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a classroom by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteClassroom(int id)
    {
        var result = await _classroomsService.DeleteClassroomAsync(id);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the classroom was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
