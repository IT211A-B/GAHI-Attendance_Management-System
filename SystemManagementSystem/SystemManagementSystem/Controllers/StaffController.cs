using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Staff;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,DepartmentHead")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StaffResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] Guid? departmentId = null, [FromQuery] string? search = null)
    {
        var result = await _staffService.GetAllAsync(page, pageSize, departmentId, search);
        return Ok(ApiResponse<PagedResult<StaffResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaffResponse>>> GetById(Guid id)
    {
        var result = await _staffService.GetByIdAsync(id);
        return Ok(ApiResponse<StaffResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StaffResponse>>> Create([FromBody] CreateStaffRequest request)
    {
        var result = await _staffService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<StaffResponse>.Ok(result, "Staff member created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaffResponse>>> Update(Guid id, [FromBody] UpdateStaffRequest request)
    {
        var result = await _staffService.UpdateAsync(id, request);
        return Ok(ApiResponse<StaffResponse>.Ok(result, "Staff member updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _staffService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Staff member deleted."));
    }

    [HttpPost("{id:guid}/regenerate-qr")]
    public async Task<ActionResult<ApiResponse<StaffResponse>>> RegenerateQrCode(Guid id)
    {
        var result = await _staffService.RegenerateQrCodeAsync(id);
        return Ok(ApiResponse<StaffResponse>.Ok(result, "QR code regenerated."));
    }
}
