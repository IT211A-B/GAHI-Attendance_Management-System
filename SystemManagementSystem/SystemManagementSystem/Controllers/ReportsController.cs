using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Reports;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,DepartmentHead")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Get a daily attendance report for a specific date.
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<DailyReportResponse>>> GetDailyReport(
        [FromQuery] DateTime? date = null)
    {
        var reportDate = date ?? DateTime.UtcNow;
        var result = await _reportService.GetDailyReportAsync(reportDate);
        return Ok(ApiResponse<DailyReportResponse>.Ok(result));
    }

    /// <summary>
    /// Get a weekly attendance report starting from a specific date.
    /// </summary>
    [HttpGet("weekly")]
    public async Task<ActionResult<ApiResponse<WeeklyReportResponse>>> GetWeeklyReport(
        [FromQuery] DateTime? startDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1);
        var result = await _reportService.GetWeeklyReportAsync(start);
        return Ok(ApiResponse<WeeklyReportResponse>.Ok(result));
    }

    /// <summary>
    /// Get attendance summary broken down by department.
    /// </summary>
    [HttpGet("department-summary")]
    public async Task<ActionResult<ApiResponse<List<DepartmentAttendanceSummary>>>> GetDepartmentSummary(
        [FromQuery] DateTime? date = null)
    {
        var reportDate = date ?? DateTime.UtcNow;
        var result = await _reportService.GetDepartmentSummaryAsync(reportDate);
        return Ok(ApiResponse<List<DepartmentAttendanceSummary>>.Ok(result));
    }
}
