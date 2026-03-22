using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing academic years (CRUD operations)
[Route("api/[controller]")]
public class AcademicYearsController : BaseController
{
    // Service for handling academic year business logic
    private readonly IAcademicYearsService _academicYearsService;
    private readonly ILogger<AcademicYearsController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public AcademicYearsController(IAcademicYearsService academicYearsService, ILogger<AcademicYearsController> logger)
    {
        _academicYearsService = academicYearsService;
        _logger = logger;
    }

    // Get all academic years - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<AcademicYearDto>>>> GetAllAcademicYears()
    {
        var result = await _academicYearsService.GetAllAcademicYearsAsync();
        return Ok(result);
    }

    // Get a specific academic year by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<AcademicYearDto>>> GetAcademicYearById(int id)
    {
        var result = await _academicYearsService.GetAcademicYearByIdAsync(id);

        // Return 404 if academic year not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new academic year
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<AcademicYearDto>>> CreateAcademicYear([FromBody] CreateAcademicYearRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<AcademicYearDto>();
        }

        var result = await _academicYearsService.CreateAcademicYearAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetAcademicYearById), new { id = result.Data!.Id }, result);
    }

    // Update an existing academic year
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<AcademicYearDto>>> UpdateAcademicYear(int id, [FromBody] UpdateAcademicYearRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<AcademicYearDto>();
        }

        var result = await _academicYearsService.UpdateAcademicYearAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the academic year was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete an academic year by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAcademicYear(int id)
    {
        var result = await _academicYearsService.DeleteAcademicYearAsync(id);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the academic year was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Activate an academic year (set as current active year)
    [HttpPost("{id}/activate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<AcademicYearDto>>> ActivateAcademicYear(int id)
    {
        var result = await _academicYearsService.ActivateAcademicYearAsync(id);

        // Return 404 if academic year not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
