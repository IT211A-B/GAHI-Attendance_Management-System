using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing courses (CRUD operations)
[Route("api/[controller]")]
public class CoursesController : BaseController
{
    // Service for handling course business logic
    private readonly ICoursesService _coursesService;
    private readonly ILogger<CoursesController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public CoursesController(ICoursesService coursesService, ILogger<CoursesController> logger)
    {
        _coursesService = coursesService;
        _logger = logger;
    }

    // Get all courses - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<CourseDto>>>> GetAllCourses()
    {
        var result = await _coursesService.GetAllCoursesAsync();
        return Ok(result);
    }

    // Get a specific course by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> GetCourseById(int id)
    {
        var result = await _coursesService.GetCourseByIdAsync(id);

        // Return 404 if course not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new course
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<CourseDto>();
        }

        var result = await _coursesService.CreateCourseAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Data!.Id }, result);
    }

    // Update an existing course
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<CourseDto>();
        }

        var result = await _coursesService.UpdateCourseAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the course was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a course by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCourse(int id)
    {
        var result = await _coursesService.DeleteCourseAsync(id);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the course was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
