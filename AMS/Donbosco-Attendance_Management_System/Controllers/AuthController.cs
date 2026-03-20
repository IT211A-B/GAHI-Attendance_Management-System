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

    // authenticate user and return jwt token
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

    // get current user profile from jwt token
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

    // logout endpoint - client should discard the token
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // jwt tokens are stateless so actual logout happens client-side

        return Ok(ApiResponse.SuccessResponse());
    }
}