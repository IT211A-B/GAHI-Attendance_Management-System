using System.Security.Claims;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for authentication operations (login, register, logout)
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IAntiforgery _antiforgery;

    public AuthController(
        IAuthService authService,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger,
        IAntiforgery antiforgery)
    {
        _authService = authService;
        _signInManager = signInManager;
        _logger = logger;
        _antiforgery = antiforgery;
    }

    // Gets an antiforgery token for CSRF protection in API requests
    [HttpGet("antiforgery-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAntiforgeryToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    // Authenticates a user with email and password
    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(ErrorCodes.Unauthorized, result.Message ?? "Login failed"));
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Login succeeded in AuthService but cookie sign-in failed for {Email}.", request.Email);
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(ErrorCodes.Unauthorized, "Login failed"));
        }

        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
    }

    // Registers a new student account
    [HttpPost("register/student")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
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

        var signInUser = await _signInManager.UserManager.FindByEmailAsync(request.Email);
        if (signInUser != null)
        {
            await _signInManager.SignInAsync(signInUser, isPersistent: false);
        }
        else
        {
            _logger.LogWarning("Registration succeeded but user lookup failed for auto sign-in: {Email}.", request.Email);
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
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
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

        var signInUser = await _signInManager.UserManager.FindByEmailAsync(request.Email);
        if (signInUser != null)
        {
            await _signInManager.SignInAsync(signInUser, isPersistent: false);
        }
        else
        {
            _logger.LogWarning("Registration succeeded but user lookup failed for auto sign-in: {Email}.", request.Email);
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
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        // Extract user ID from Identity claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse(ErrorCodes.Unauthorized, "Unable to identify user"));
        }

        var profile = await _authService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse(ErrorCodes.NotFound, "User profile not found"));
        }

        return Ok(ApiResponse<UserDto>.SuccessResponse(profile));
    }

    // Logs out the current user by clearing the auth cookie
    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<string>>> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully."));
    }
}
