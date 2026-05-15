using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("attendance/checklist")]
public class SectionAttendanceController : Controller
{
    private readonly ISectionPageService _sectionPageService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<SectionAttendanceController> _logger;

    public SectionAttendanceController(
        ISectionPageService sectionPageService,
        IAttendanceService attendanceService,
        ILogger<SectionAttendanceController> logger)
    {
        _sectionPageService = sectionPageService;
        _attendanceService = attendanceService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? attendanceDate)
    {
        var context = GetUserContext();
        if (!context.IsValid) return Challenge();

        SectionAttendanceIndexViewModel viewModel;
        try
        {
            viewModel = await _sectionPageService.BuildSectionAttendanceIndexViewModelAsync(context.UserId, context.Role, sectionId, scheduleId, attendanceDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section attendance index view.");
            viewModel = new SectionAttendanceIndexViewModel { ErrorMessage = "Unable to load attendance data right now." };
        }
        return View(viewModel);
    }

    [HttpPost("mark")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSectionAttendance([FromForm] SectionMarkAttendanceFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid) return Challenge();
        if (!ModelState.IsValid) { TempData["SectionAttendanceError"] = "Please provide valid attendance details."; return RedirectToAction(nameof(Index), new { sectionId = form.SectionId, scheduleId = form.ScheduleId, attendanceDate = form.Date.ToString("yyyy-MM-dd") }); }
        var teacherContextResult = await _sectionPageService.BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContextResult.Success) { TempData["SectionAttendanceError"] = teacherContextResult.Error ?? "Unable to identify teacher context."; return RedirectToAction(nameof(Index), new { sectionId = form.SectionId, scheduleId = form.ScheduleId, attendanceDate = form.Date.ToString("yyyy-MM-dd") }); }
        try
        {
            await _attendanceService.MarkAttendanceAsync(
                new MarkAttendanceRequest
                {
                    SectionId = form.SectionId,
                    ScheduleId = form.ScheduleId,
                    StudentId = form.StudentId,
                    Date = form.Date,
                    TimeIn = form.TimeIn,
                    Remarks = NormalizeOptional(form.Remarks)
                },
                teacherContextResult.Context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark attendance for student {StudentId}.", form.StudentId);
            TempData["SectionAttendanceError"] = BuildAttendanceErrorMessage(ex);
            return RedirectToAction(nameof(Index), new { sectionId = form.SectionId, scheduleId = form.ScheduleId, attendanceDate = form.Date.ToString("yyyy-MM-dd") });
        }

        TempData["SectionAttendanceSuccess"] = "Attendance marked successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = form.SectionId, scheduleId = form.ScheduleId, attendanceDate = form.Date.ToString("yyyy-MM-dd") });
    }

    private static string BuildAttendanceErrorMessage(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException
                when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
            _ => "Unable to mark attendance right now."
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private (bool IsValid, int UserId, string Role) GetUserContext()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!int.TryParse(idClaim, out var uid) || string.IsNullOrWhiteSpace(role))
            return (false, 0, string.Empty);
        return (true, uid, role);
    }
}
