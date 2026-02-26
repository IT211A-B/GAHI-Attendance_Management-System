using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Departments;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DepartmentResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _departmentService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<DepartmentResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> GetById(Guid id)
    {
        var result = await _departmentService.GetByIdAsync(id);
        return Ok(ApiResponse<DepartmentResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> Create([FromBody] CreateDepartmentRequest request)
    {
        var result = await _departmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<DepartmentResponse>.Ok(result, "Department created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _departmentService.UpdateAsync(id, request);
        return Ok(ApiResponse<DepartmentResponse>.Ok(result, "Department updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _departmentService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Department deleted."));
    }
}
