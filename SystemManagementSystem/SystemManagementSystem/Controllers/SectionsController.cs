using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Sections;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SectionsController : ControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionsController(ISectionService sectionService)
    {
        _sectionService = sectionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SectionResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] Guid? programId = null, [FromQuery] Guid? periodId = null)
    {
        var result = await _sectionService.GetAllAsync(page, pageSize, programId, periodId);
        return Ok(ApiResponse<PagedResult<SectionResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SectionResponse>>> GetById(Guid id)
    {
        var result = await _sectionService.GetByIdAsync(id);
        return Ok(ApiResponse<SectionResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<ActionResult<ApiResponse<SectionResponse>>> Create([FromBody] CreateSectionRequest request)
    {
        var result = await _sectionService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<SectionResponse>.Ok(result, "Section created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Registrar")]
    public async Task<ActionResult<ApiResponse<SectionResponse>>> Update(Guid id, [FromBody] UpdateSectionRequest request)
    {
        var result = await _sectionService.UpdateAsync(id, request);
        return Ok(ApiResponse<SectionResponse>.Ok(result, "Section updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _sectionService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Section deleted."));
    }
}
