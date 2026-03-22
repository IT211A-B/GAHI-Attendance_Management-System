using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Helper method to format validation errors into a standardized API response.
    /// </summary>
    /// <typeparam name="T">The type of data expected in the response</typeparam>
    /// <returns>BadRequest with formatted validation error response</returns>
    protected ActionResult<ApiResponse<T>> ValidationError<T>()
    {
        var errors = string.Join(", ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        return BadRequest(ApiResponse<T>.ErrorResponse(ErrorCodes.ValidationError, errors));
    }
}