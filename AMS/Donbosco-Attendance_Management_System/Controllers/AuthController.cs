using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Services;

namespace Donbosco_Attendance_Management_System.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token with user profile</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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

        var (response, errorCode, errorMessage) = await _authService.LoginAsync(request);

        if (errorCode != null)
        {
            var statusCode = errorCode switch
            {
                ErrorCodes.INVALID_CREDENTIALS => StatusCodes.Status401Unauthorized,
                ErrorCodes.USER_INACTIVE => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(response!));
    }

    /// <summary>
    /// Get current user profile from JWT token
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = HttpContext.Items["UserId"] as Guid?;

        if (userId == null)
        {
            return Unauthorized(ApiResponse.FailureResponse(
                ErrorCodes.UNAUTHORIZED,
                "Authentication required"
            ));
        }

        var userProfile = await _authService.GetCurrentUserAsync(userId.Value);

        if (userProfile == null)
        {
            return NotFound(ApiResponse.FailureResponse(
                ErrorCodes.NOT_FOUND,
                "User not found"
            ));
        }

        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(userProfile));
    }

    /// <summary>
    /// Logout - client should discard the token
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // JWT tokens are stateless - actual logout happens client-side
        // by removing the token from storage
        // This endpoint exists for API completeness and potential future
        // token blacklisting implementation

        return Ok(ApiResponse.SuccessResponse());
    }
}