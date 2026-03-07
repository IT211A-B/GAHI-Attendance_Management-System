using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPrograms;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AcademicProgramsController : ControllerBase
{
    private readonly IAcademicProgramService _programService;

    public AcademicProgramsController(IAcademicProgramService programService)
    {
        _programService = programService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AcademicProgramResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? departmentId = null)
    {
        var result = await _programService.GetAllAsync(page, pageSize, departmentId);
        return Ok(ApiResponse<PagedResult<AcademicProgramResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AcademicProgramResponse>>> GetById(Guid id)
    {
        var result = await _programService.GetByIdAsync(id);
        return Ok(ApiResponse<AcademicProgramResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<ActionResult<ApiResponse<AcademicProgramResponse>>> Create([FromBody] CreateAcademicProgramRequest request)
    {
        var result = await _programService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<AcademicProgramResponse>.Ok(result, "Academic program created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<ActionResult<ApiResponse<AcademicProgramResponse>>> Update(Guid id, [FromBody] UpdateAcademicProgramRequest request)
    {
        var result = await _programService.UpdateAsync(id, request);
        return Ok(ApiResponse<AcademicProgramResponse>.Ok(result, "Academic program updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _programService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Academic program deleted."));
    }
}
