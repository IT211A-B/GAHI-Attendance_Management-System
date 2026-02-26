using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Attendance;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Process a QR code scan from a gate terminal.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Roles = "Admin,Guard")]
    public async Task<ActionResult<ApiResponse<ScanResponse>>> Scan([FromBody] ScanRequest request)
    {
        var result = await _attendanceService.ProcessScanAsync(request);
        return Ok(ApiResponse<ScanResponse>.Ok(result, "Scan processed successfully."));
    }

    /// <summary>
    /// Query attendance logs with filters (date, section, status, person type).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Registrar,DepartmentHead")]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendanceLogResponse>>>> GetLogs(
        [FromQuery] AttendanceFilterRequest filter)
    {
        var result = await _attendanceService.GetLogsAsync(filter);
        return Ok(ApiResponse<PagedResult<AttendanceLogResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Registrar,DepartmentHead")]
    public async Task<ActionResult<ApiResponse<AttendanceLogResponse>>> GetById(Guid id)
    {
        var result = await _attendanceService.GetByIdAsync(id);
        return Ok(ApiResponse<AttendanceLogResponse>.Ok(result));
    }
}
