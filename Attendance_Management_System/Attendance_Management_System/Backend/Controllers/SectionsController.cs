using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing sections (CRUD operations)
[Route("api/[controller]")]
public class SectionsController : BaseController
{
    // Service for handling section business logic
    private readonly ISectionsService _sectionsService;
    private readonly ILogger<SectionsController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public SectionsController(ISectionsService sectionsService, ILogger<SectionsController> logger)
    {
        _sectionsService = sectionsService;
        _logger = logger;
    }

    // Get all sections - Admin or Teacher access
    [HttpGet]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<ActionResult<ApiResponse<List<SectionDto>>>> GetAllSections()
    {
        var result = await _sectionsService.GetAllSectionsAsync();
        return Ok(result);
    }

    // Get a specific section by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<ActionResult<ApiResponse<SectionDto>>> GetSectionById(int id)
    {
        var result = await _sectionsService.GetSectionByIdAsync(id);

        // Return 404 if section not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Get all sections for a specific academic year
    [HttpGet("academic-year/{academicYearId}")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<ActionResult<ApiResponse<List<SectionDto>>>> GetSectionsByAcademicYear(int academicYearId)
    {
        var result = await _sectionsService.GetSectionsByAcademicYearIdAsync(academicYearId);

        // Return 404 if academic year not found or has no sections
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Get timetable for a section - Admin or Teacher access
    [HttpGet("{id}/timetable")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<ActionResult<ApiResponse<TimetableResponse>>> GetTimetable(int id)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _sectionsService.GetTimetableAsync(id, currentUserId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Get all teachers assigned to a section - Admin only
    [HttpGet("{id}/teachers")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<SectionTeacherDto>>>> GetSectionTeachers(int id)
    {
        var result = await _sectionsService.GetSectionTeachersAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Assign a teacher to a section - Admin only
    [HttpPost("{id}/teachers")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<SectionTeacherDto>>> AssignTeacherToSection(int id, [FromBody] AssignTeacherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError<SectionTeacherDto>();
        }

        var result = await _sectionsService.AssignTeacherToSectionAsync(id, request);

        if (!result.Success)
        {
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            if (result.Error?.Code == ErrorCodes.DuplicateAssignment)
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetSectionTeachers), new { id = id }, result);
    }

    // Remove a teacher from a section - Admin or Teacher access
    [HttpDelete("{id}/teachers/{teacherId}")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveTeacherFromSection(int id, int teacherId)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var isAdmin = userRole == "admin";

        var result = await _sectionsService.RemoveTeacherFromSectionAsync(id, teacherId, isAdmin);

        if (!result.Success)
        {
            if (result.Error?.Code == ErrorCodes.TeacherHasSchedules)
            {
                return BadRequest(result);
            }
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new section
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<SectionDto>>> CreateSection([FromBody] CreateSectionRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<SectionDto>();
        }

        var result = await _sectionsService.CreateSectionAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetSectionById), new { id = result.Data!.Id }, result);
    }

    // Update an existing section
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<SectionDto>>> UpdateSection(int id, [FromBody] UpdateSectionRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<SectionDto>();
        }

        var result = await _sectionsService.UpdateSectionAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the section was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a section by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSection(int id)
    {
        var result = await _sectionsService.DeleteSectionAsync(id);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the section was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
