using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing subjects (CRUD operations)
[Route("api/[controller]")]
public class SubjectsController : BaseController
{
    // Service for handling subject business logic
    private readonly ISubjectsService _subjectsService;
    private readonly ILogger<SubjectsController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public SubjectsController(ISubjectsService subjectsService, ILogger<SubjectsController> logger)
    {
        _subjectsService = subjectsService;
        _logger = logger;
    }

    // Get all subjects - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<SubjectDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SubjectDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<SubjectDto>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<SubjectDto>>>> GetAllSubjects()
    {
        var result = await _subjectsService.GetAllSubjectsAsync();
        return Ok(result);
    }

    // Get a specific subject by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubjectDto>>> GetSubjectById(int id)
    {
        var result = await _subjectsService.GetSubjectByIdAsync(id);

        // Return 404 if subject not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Get all subjects for a specific course
    [HttpGet("course/{courseId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<SubjectDto>>>> GetSubjectsByCourse(int courseId)
    {
        var result = await _subjectsService.GetSubjectsByCourseIdAsync(courseId);

        // Return 404 if course not found or has no subjects
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new subject
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SubjectDto>>> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<SubjectDto>();
        }

        var result = await _subjectsService.CreateSubjectAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetSubjectById), new { id = result.Data!.Id }, result);
    }

    // Update an existing subject
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SubjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubjectDto>>> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<SubjectDto>();
        }

        var result = await _subjectsService.UpdateSubjectAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the subject was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a subject by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSubject(int id)
    {
        var result = await _subjectsService.DeleteSubjectAsync(id);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the subject was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
