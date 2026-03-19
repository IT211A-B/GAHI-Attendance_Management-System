using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Services;
using Donbosco_Attendance_Management_System.Middleware;

namespace Donbosco_Attendance_Management_System.Controllers;

[ApiController]
[Route("api/classrooms")]
public class ClassroomsController : ControllerBase
{
    private readonly IClassroomsService _classroomsService;
    private readonly ILogger<ClassroomsController> _logger;

    public ClassroomsController(IClassroomsService classroomsService, ILogger<ClassroomsController> logger)
    {
        _classroomsService = classroomsService;
        _logger = logger;
    }

    // list all classrooms
    [HttpGet]
    public async Task<IActionResult> GetAllClassrooms()
    {
        var result = await _classroomsService.GetAllClassroomsAsync();
        return Ok(ApiResponse<ListResponse<ClassroomResponse>>.SuccessResponse(result));
    }

    // create a new classroom
    [HttpPost]
    [RequireRole("admin")]
    public async Task<IActionResult> CreateClassroom([FromBody] CreateClassroomRequest request)
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

        var (classroom, errorCode, errorMessage) = await _classroomsService.CreateClassroomAsync(request);

        if (errorCode != null)
        {
            var statusCode = errorCode == ErrorCodes.VALIDATION_ERROR
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return CreatedAtAction(
            nameof(GetClassroomById),
            new { id = classroom!.Id },
            ApiResponse<ClassroomResponse>.SuccessResponse(classroom)
        );
    }

    // get a classroom by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassroomById(Guid id)
    {
        var classroom = await _classroomsService.GetClassroomByIdAsync(id);

        if (classroom == null)
        {
            return NotFound(ApiResponse.FailureResponse(
                ErrorCodes.NOT_FOUND,
                "Classroom not found"
            ));
        }

        return Ok(ApiResponse<ClassroomResponse>.SuccessResponse(classroom));
    }

    // update a classroom
    [HttpPut("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> UpdateClassroom(Guid id, [FromBody] UpdateClassroomRequest request)
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

        var (classroom, errorCode, errorMessage) = await _classroomsService.UpdateClassroomAsync(id, request);

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

        return Ok(ApiResponse<ClassroomResponse>.SuccessResponse(classroom!));
    }

    // delete a classroom
    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> DeleteClassroom(Guid id)
    {
        var (success, errorCode, errorMessage) = await _classroomsService.DeleteClassroomAsync(id);

        if (!success)
        {
            var statusCode = errorCode switch
            {
                ErrorCodes.NOT_FOUND => StatusCodes.Status404NotFound,
                ErrorCodes.CONFLICT_CLASSROOM => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode!, errorMessage!));
        }

        return Ok(ApiResponse.SuccessResponse());
    }
}