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

    public SectionAttendanceController(
        ISectionPageService sectionPageService,
        IAttendanceService attendanceService)
    {
        _sectionPageService = sectionPageService;
        _attendanceService = attendanceService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? attendanceDate)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await _sectionPageService.BuildSectionAttendanceIndexViewModelAsync(
            context.UserId,
            context.Role,
            sectionId,
            scheduleId,
            attendanceDate);
        return View(viewModel);
    }

    [HttpPost("mark")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSectionAttendance([FromForm] SectionMarkAttendanceFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["SectionAttendanceError"] = "Please provide valid attendance details.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        var teacherContextResult = await _sectionPageService.BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContextResult.Success)
        {
            TempData["SectionAttendanceError"] = teacherContextResult.Error ?? "Unable to identify teacher context.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        var result = await ExecuteServiceCallAsync(() => _attendanceService.MarkAttendanceAsync(new MarkAttendanceRequest
        {
            SectionId = form.SectionId,
            ScheduleId = form.ScheduleId,
            StudentId = form.StudentId,
            Date = form.Date,
            TimeIn = form.TimeIn,
            Remarks = NormalizeOptional(form.Remarks)
        }, teacherContextResult.Context));

        if (!result.Success)
        {
            TempData["SectionAttendanceError"] = result.Error?.Message ?? "Unable to mark attendance right now.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        TempData["SectionAttendanceSuccess"] = "Attendance marked successfully.";
        return RedirectToAction(nameof(Index), new
        {
            sectionId = form.SectionId,
            scheduleId = form.ScheduleId,
            attendanceDate = form.Date.ToString("yyyy-MM-dd")
        });
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private (bool IsValid, int UserId, string Role) GetUserContext()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return (false, 0, string.Empty);
        }

        return (true, userId, role);
    }
}

