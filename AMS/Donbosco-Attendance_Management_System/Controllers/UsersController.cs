using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Services;
using Donbosco_Attendance_Management_System.Middleware;

namespace Donbosco_Attendance_Management_System.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    // list all users
    [HttpGet]
    [RequireRole("admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _usersService.GetAllUsersAsync();
        return Ok(ApiResponse<UserListResponse>.SuccessResponse(result));
    }

    // create a new user
    [HttpPost]
    [RequireRole("admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
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

        var (user, errorCode, errorMessage) = await _usersService.CreateUserAsync(request);

        if (errorCode != null)
        {
            var statusCode = errorCode == ErrorCodes.VALIDATION_ERROR
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return CreatedAtAction(
            nameof(GetUserById),
            new { id = user!.Id },
            ApiResponse<UserProfileResponse>.SuccessResponse(user)
        );
    }

    // get a user by id
    [HttpGet("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _usersService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(ApiResponse.FailureResponse(
                ErrorCodes.NOT_FOUND,
                "User not found"
            ));
        }

        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(user));
    }

    // update a user
    [HttpPut("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
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

        var (user, errorCode, errorMessage) = await _usersService.UpdateUserAsync(id, request);

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

        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(user!));
    }

    // delete a user
    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var (success, errorCode, errorMessage) = await _usersService.DeleteUserAsync(id);

        if (!success)
        {
            return NotFound(ApiResponse.FailureResponse(errorCode!, errorMessage!));
        }

        return Ok(ApiResponse.SuccessResponse());
    }

    // update own profile
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;

        if (userId == null)
        {
            return Unauthorized(ApiResponse.FailureResponse(
                ErrorCodes.UNAUTHORIZED,
                "Authentication required"
            ));
        }

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

        var (user, errorCode, errorMessage) = await _usersService.UpdateProfileAsync(userId.Value, request);

        if (errorCode != null)
        {
            return NotFound(ApiResponse.FailureResponse(errorCode, errorMessage!));
        }

        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(user!));
    }
}