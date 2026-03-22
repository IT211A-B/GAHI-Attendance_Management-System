using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance_Management_System.Backend.Controllers;

// Base controller providing common functionality for all API controllers
[ApiController]
public abstract class BaseController : ControllerBase
{
    // Formats model validation errors into a standardized API error response
    protected ActionResult<ApiResponse<T>> ValidationError<T>()
    {
        // Combine all validation error messages into a single string
        var errors = string.Join(", ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        return BadRequest(ApiResponse<T>.ErrorResponse(ErrorCodes.ValidationError, errors));
    }

    // Extracts the current authenticated user's ID from JWT token claims
    protected int? GetCurrentUserId()
    {
        // Look for the user ID claim in the token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }
}
