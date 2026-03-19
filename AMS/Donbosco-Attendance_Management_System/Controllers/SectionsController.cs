using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Services;
using Donbosco_Attendance_Management_System.Middleware;

namespace Donbosco_Attendance_Management_System.Controllers;

[ApiController]
[Route("api/sections")]
public class SectionsController : ControllerBase
{
    private readonly ISectionsService _sectionsService;
    private readonly ILogger<SectionsController> _logger;

    public SectionsController(ISectionsService sectionsService, ILogger<SectionsController> logger)
    {
        _sectionsService = sectionsService;
        _logger = logger;
    }

    // list all sections
    [HttpGet]
    public async Task<IActionResult> GetAllSections()
    {
        var result = await _sectionsService.GetAllSectionsAsync();
        return Ok(ApiResponse<ListResponse<SectionResponse>>.SuccessResponse(result));
    }

    // create a new section
    [HttpPost]
    [RequireRole("admin")]
    public async Task<IActionResult> CreateSection([FromBody] CreateSectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(ApiResponse.FailureResponse(
                ErrorCodes.VALIDATION_ERROR,
                "Validation failed",
                errors
            ));
        }

        var (section, errorCode, errorMessage) = await _sectionsService.CreateSectionAsync(request);

        if (errorCode != null)
        {
            var statusCode = errorCode == ErrorCodes.VALIDATION_ERROR
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return CreatedAtAction(
            nameof(GetSectionById),
            new { id = section!.Id },
            ApiResponse<SectionResponse>.SuccessResponse(section)
        );
    }

    // get a section by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSectionById(Guid id)
    {
        var section = await _sectionsService.GetSectionByIdAsync(id);

        if (section == null)
        {
            return NotFound(ApiResponse.FailureResponse(
                ErrorCodes.NOT_FOUND,
                "Section not found"
            ));
        }

        return Ok(ApiResponse<SectionResponse>.SuccessResponse(section));
    }

    // update a section
    [HttpPut("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> UpdateSection(Guid id, [FromBody] UpdateSectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(ApiResponse.FailureResponse(
                ErrorCodes.VALIDATION_ERROR,
                "Validation failed",
                errors
            ));
        }

        var (section, errorCode, errorMessage) = await _sectionsService.UpdateSectionAsync(id, request);

        if (errorCode != null)
        {
            var statusCode = errorCode switch
            {
                ErrorCodes.NOT_FOUND => StatusCodes.Status404NotFound,
                ErrorCodes.VALIDATION_ERROR => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return Ok(ApiResponse<SectionResponse>.SuccessResponse(section!));
    }

    // delete a section
    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> DeleteSection(Guid id)
    {
        var (success, errorCode, errorMessage) = await _sectionsService.DeleteSectionAsync(id);

        if (!success)
        {
            var statusCode = errorCode switch
            {
                ErrorCodes.NOT_FOUND => StatusCodes.Status404NotFound,
                ErrorCodes.CONFLICT_SECTION_SLOT => StatusCodes.Status409Conflict,
                ErrorCodes.VALIDATION_ERROR => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode!, errorMessage!));
        }

        return Ok(ApiResponse.SuccessResponse());
    }
}