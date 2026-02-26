using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPeriods;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AcademicPeriodsController : ControllerBase
{
    private readonly IAcademicPeriodService _periodService;

    public AcademicPeriodsController(IAcademicPeriodService periodService)
    {
        _periodService = periodService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AcademicPeriodResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _periodService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<AcademicPeriodResponse>>.Ok(result));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<AcademicPeriodResponse>>> GetCurrent()
    {
        var result = await _periodService.GetCurrentAsync();
        return Ok(ApiResponse<AcademicPeriodResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AcademicPeriodResponse>>> GetById(Guid id)
    {
        var result = await _periodService.GetByIdAsync(id);
        return Ok(ApiResponse<AcademicPeriodResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AcademicPeriodResponse>>> Create([FromBody] CreateAcademicPeriodRequest request)
    {
        var result = await _periodService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<AcademicPeriodResponse>.Ok(result, "Academic period created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AcademicPeriodResponse>>> Update(Guid id, [FromBody] UpdateAcademicPeriodRequest request)
    {
        var result = await _periodService.UpdateAsync(id, request);
        return Ok(ApiResponse<AcademicPeriodResponse>.Ok(result, "Academic period updated."));
    }

    [HttpPut("{id:guid}/set-current")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AcademicPeriodResponse>>> SetCurrent(Guid id)
    {
        var result = await _periodService.SetCurrentAsync(id);
        return Ok(ApiResponse<AcademicPeriodResponse>.Ok(result, "Academic period set as current."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _periodService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Academic period deleted."));
    }
}
