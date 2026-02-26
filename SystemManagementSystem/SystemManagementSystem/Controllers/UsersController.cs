using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Users;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<UserResponse>.Ok(result, "User created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);
        return Ok(ApiResponse<UserResponse>.Ok(result, "User updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> AssignRoles(Guid id, [FromBody] AssignRolesRequest request)
    {
        var result = await _userService.AssignRolesAsync(id, request);
        return Ok(ApiResponse<UserResponse>.Ok(result, "Roles assigned successfully."));
    }
}
