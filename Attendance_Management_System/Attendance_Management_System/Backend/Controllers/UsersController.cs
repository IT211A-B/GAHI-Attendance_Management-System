using System.Security.Claims;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

// API controller for managing users (CRUD operations)
[Route("api/[controller]")]
public class UsersController : BaseController
{
    // Service for handling user business logic
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;

    // Initialize controller with required dependencies via dependency injection
    public UsersController(IUsersService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    // Get all users - Admin only access
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers()
    {
        var result = await _usersService.GetAllUsersAsync();
        return Ok(result);
    }

    // Get a specific user by ID
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
    {
        var result = await _usersService.GetUserByIdAsync(id);

        // Return 404 if user not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Create a new user
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<UserDto>();
        }

        var result = await _usersService.CreateUserAsync(request);

        // Return 400 if creation failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 with location header pointing to the new resource
        return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result);
    }

    // Update an existing user (admin operation)
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<UserDto>();
        }

        var result = await _usersService.UpdateUserAsync(id, request);

        // Handle different error scenarios
        if (!result.Success)
        {
            // Check if the user was not found
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Delete a user by ID
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
    {
        var result = await _usersService.DeleteUserAsync(id);

        // Return 404 if user not found
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Update the current authenticated user's own profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // Validate request model before processing
        if (!ModelState.IsValid)
        {
            return ValidationError<UserDto>();
        }

        // Extract user ID from JWT token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse(ErrorCodes.Unauthorized, "Unable to identify user."));
        }

        var result = await _usersService.UpdateProfileAsync(userId, request);

        // Return 400 if update failed
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
