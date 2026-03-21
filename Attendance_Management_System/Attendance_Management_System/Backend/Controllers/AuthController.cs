using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for authentication operations (login, register, logout)
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // Authenticates a user with email and password
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse("UNAUTHORIZED", result.Message ?? "Login failed"));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
    }

    // Registers a new student account
    [HttpPost("register/student")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterStudent([FromBody] RegisterRequest request)
    {
        if (ModelState.IsValid is false)
        {
            return ValidationError<AuthResponse>();
        }

        var result = await _authService.RegisterStudentAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("REGISTRATION_FAILED", result.Message ?? "Registration failed"));
        }

        return CreatedAtAction(
            nameof(GetProfile),
            null,
            ApiResponse<AuthResponse>.SuccessResponse(result)
        );
    }

    // Registers a new teacher account
    [HttpPost("register/teacher")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterTeacher([FromBody] TeacherRegisterRequest request)
    {
        if (ModelState.IsValid is false)
        {
            return ValidationError<AuthResponse>();
        }

        var result = await _authService.RegisterTeacherAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("REGISTRATION_FAILED", result.Message ?? "Registration failed"));
        }

        return CreatedAtAction(
            nameof(GetProfile),
            null,
            ApiResponse<AuthResponse>.SuccessResponse(result)
        );
    }

    // Gets the current authenticated user's profile information
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        // Extract user ID from JWT token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("UNAUTHORIZED", "Invalid token"));
        }

        var profile = await _authService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse("NOT_FOUND", "User profile not found"));
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(profile));
    }

    // Logs out the current user (client-side token invalidation)
    // JWT is stateless, so actual token invalidation requires a blacklist (out of MVP scope)
    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse<string>> Logout()
    {
        // JWT is stateless, so we just return a success message
        // Token invalidation would require a token blacklist (out of MVP scope)
        return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully. Please discard your token on the client side."));
    }

    // Creates a validation error response from ModelState errors
    private ActionResult<ApiResponse<T>> ValidationError<T>()
    {
        var errors = string.Join(", ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        return BadRequest(ApiResponse<T>.ErrorResponse("VALIDATION_ERROR", errors));
    }
}