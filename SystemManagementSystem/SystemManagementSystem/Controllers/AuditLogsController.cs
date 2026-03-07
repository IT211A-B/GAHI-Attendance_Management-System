using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.AuditLogs;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Query audit logs with filters. Read-only endpoint.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> GetAll(
        [FromQuery] AuditLogFilterRequest filter)
    {
        var result = await _auditLogService.GetLogsAsync(filter);
        return Ok(ApiResponse<PagedResult<AuditLogResponse>>.Ok(result));
    }
}
