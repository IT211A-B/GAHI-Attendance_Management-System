using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("attendance")]
public class AttendanceManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly ISchedulesService _schedulesService;
    private readonly IAttendanceService _attendanceService;

    public AttendanceManagementController(
        ISectionsService sectionsService,
        ISchedulesService schedulesService,
        IAttendanceService attendanceService)
    {
        _sectionsService = sectionsService;
        _schedulesService = schedulesService;
        _attendanceService = attendanceService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? date)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = new AttendanceIndexViewModel
        {
            SelectedSectionId = sectionId,
            SelectedScheduleId = scheduleId,
            SelectedDate = selectedDate
        };

        var sectionsResult = await _sectionsService.GetAllSectionsAsync();
        if (!sectionsResult.Success || sectionsResult.Data is null)
        {
            model.ErrorMessage = sectionsResult.Error?.Message ?? "Unable to load sections.";
            return View(model);
        }

        model.Sections = sectionsResult.Data
            .OrderBy(s => s.Name)
            .Select(s => new AttendanceSectionOptionViewModel { Id = s.Id, Name = s.Name })
            .ToList();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return Challenge();
        }

        var schedulesResult = await _schedulesService.GetSchedulesAsync(userId, role);
        if (!schedulesResult.Success || schedulesResult.Data is null)
        {
            model.ErrorMessage = schedulesResult.Error?.Message ?? "Unable to load schedules.";
            return View(model);
        }

        model.Schedules = schedulesResult.Data
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new AttendanceScheduleOptionViewModel
            {
                Id = s.Id,
                SectionId = s.SectionId,
                Label = $"{s.SectionName} | {s.SubjectName} | {s.DayName} {s.StartTime}-{s.EndTime}"
            })
            .ToList();

        if (sectionId is null || scheduleId is null)
        {
            return View(model);
        }

        var summaryResult = await _attendanceService.GetSectionAttendanceAsync(sectionId.Value, selectedDate, scheduleId.Value);
        if (!summaryResult.Success || summaryResult.Data is null)
        {
            model.ErrorMessage = summaryResult.Error?.Message ?? "Unable to load attendance summary.";
            return View(model);
        }

        model.TotalStudents = summaryResult.Data.TotalStudents;
        model.PresentCount = summaryResult.Data.PresentCount;
        model.LateCount = summaryResult.Data.LateCount;
        model.AbsentCount = summaryResult.Data.AbsentCount;
        model.Records = summaryResult.Data.Records
            .OrderBy(r => r.StudentName)
            .Select(r => new AttendanceRecordItemViewModel
            {
                StudentName = string.IsNullOrWhiteSpace(r.StudentName) ? "-" : r.StudentName,
                SubjectName = string.IsNullOrWhiteSpace(r.SubjectName) ? "-" : r.SubjectName,
                TimeIn = r.TimeIn?.ToString("HH:mm") ?? "-",
                TimeOut = r.TimeOut?.ToString("HH:mm") ?? "-",
                Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? "-" : r.Remarks,
                MarkerName = string.IsNullOrWhiteSpace(r.MarkerName) ? "-" : r.MarkerName
            })
            .ToList();

        return View(model);
    }
}
