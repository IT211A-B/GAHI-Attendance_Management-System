using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.GateTerminals;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class GateTerminalsController : ControllerBase
{
    private readonly IGateTerminalService _terminalService;

    public GateTerminalsController(IGateTerminalService terminalService)
    {
        _terminalService = terminalService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GateTerminalResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _terminalService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<GateTerminalResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GateTerminalResponse>>> GetById(Guid id)
    {
        var result = await _terminalService.GetByIdAsync(id);
        return Ok(ApiResponse<GateTerminalResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GateTerminalResponse>>> Create([FromBody] CreateGateTerminalRequest request)
    {
        var result = await _terminalService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<GateTerminalResponse>.Ok(result, "Gate terminal created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GateTerminalResponse>>> Update(Guid id, [FromBody] UpdateGateTerminalRequest request)
    {
        var result = await _terminalService.UpdateAsync(id, request);
        return Ok(ApiResponse<GateTerminalResponse>.Ok(result, "Gate terminal updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _terminalService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Gate terminal deleted."));
    }
}
