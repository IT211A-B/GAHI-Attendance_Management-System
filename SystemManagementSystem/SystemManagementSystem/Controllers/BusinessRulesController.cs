using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.BusinessRules;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BusinessRulesController : ControllerBase
{
    private readonly IBusinessRuleService _ruleService;

    public BusinessRulesController(IBusinessRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BusinessRuleResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? departmentId = null)
    {
        var result = await _ruleService.GetAllAsync(page, pageSize, departmentId);
        return Ok(ApiResponse<PagedResult<BusinessRuleResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessRuleResponse>>> GetById(Guid id)
    {
        var result = await _ruleService.GetByIdAsync(id);
        return Ok(ApiResponse<BusinessRuleResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BusinessRuleResponse>>> Create([FromBody] CreateBusinessRuleRequest request)
    {
        var result = await _ruleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<BusinessRuleResponse>.Ok(result, "Business rule created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessRuleResponse>>> Update(Guid id, [FromBody] UpdateBusinessRuleRequest request)
    {
        var result = await _ruleService.UpdateAsync(id, request);
        return Ok(ApiResponse<BusinessRuleResponse>.Ok(result, "Business rule updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _ruleService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Business rule deleted."));
    }
}
