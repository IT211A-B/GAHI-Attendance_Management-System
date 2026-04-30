using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("reports")]
public class ReportsController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly ISchedulesService _schedulesService;
    private readonly IAttendanceService _attendanceService;

    public ReportsController(
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
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return Challenge();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = new ReportsIndexViewModel
        {
            SelectedSectionId = sectionId,
            SelectedScheduleId = scheduleId,
            SelectedDate = selectedDate
        };

        var schedulesResult = await _schedulesService.GetSchedulesAsync(userId, role);
        if (!schedulesResult.Success || schedulesResult.Data is null)
        {
            model.ErrorMessage = schedulesResult.Error?.Message ?? "Unable to load schedules.";
            return View(model);
        }

        var accessibleSchedules = schedulesResult.Data
            .Where(schedule => !string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase) || schedule.IsMine)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .ToList();

        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            var sectionsResult = await _sectionsService.GetAllSectionsAsync();
            if (!sectionsResult.Success || sectionsResult.Data is null)
            {
                model.ErrorMessage = sectionsResult.Error?.Message ?? "Unable to load sections.";
                return View(model);
            }

            model.Sections = sectionsResult.Data
                .OrderBy(section => section.Name)
                .Select(section => new ReportsSectionOptionViewModel
                {
                    Id = section.Id,
                    Name = section.Name
                })
                .ToList();
        }
        else
        {
            model.Sections = accessibleSchedules
                .GroupBy(schedule => new { schedule.SectionId, schedule.SectionName })
                .OrderBy(group => group.Key.SectionName)
                .Select(group => new ReportsSectionOptionViewModel
                {
                    Id = group.Key.SectionId,
                    Name = group.Key.SectionName
                })
                .ToList();
        }

        model.Schedules = accessibleSchedules
            .Select(schedule => new ReportsScheduleOptionViewModel
            {
                Id = schedule.Id,
                SectionId = schedule.SectionId,
                Label = $"{schedule.SectionName} | {schedule.SubjectName} | {schedule.DayName} {schedule.StartTime}-{schedule.EndTime}"
            })
            .ToList();

        if (sectionId.HasValue && !model.Sections.Any(section => section.Id == sectionId.Value))
        {
            model.ErrorMessage = "You do not have access to the selected section.";
            model.SelectedSectionId = null;
            model.SelectedScheduleId = null;
            return View(model);
        }

        if (scheduleId.HasValue)
        {
            var hasAccessToSchedule = accessibleSchedules.Any(schedule =>
                schedule.Id == scheduleId.Value
                && (!sectionId.HasValue || schedule.SectionId == sectionId.Value));

            if (!hasAccessToSchedule)
            {
                model.ErrorMessage = "You do not have access to the selected schedule.";
                model.SelectedScheduleId = null;
                return View(model);
            }
        }

        if (sectionId is null || scheduleId is null)
        {
            return View(model);
        }

        var summaryResult = await _attendanceService.GetSectionAttendanceAsync(
            sectionId.Value,
            selectedDate,
            scheduleId.Value,
            userId,
            role);
        if (!summaryResult.Success || summaryResult.Data is null)
        {
            model.ErrorMessage = summaryResult.Error?.Message ?? "Unable to load report data.";
            return View(model);
        }

        model.TotalStudents = summaryResult.Data.TotalStudents;
        model.PresentCount = summaryResult.Data.PresentCount;
        model.LateCount = summaryResult.Data.LateCount;
        model.AbsentCount = summaryResult.Data.AbsentCount;
        model.UnmarkedCount = summaryResult.Data.UnmarkedCount;

        if (model.TotalStudents > 0)
        {
            model.PresentRate = decimal.Round((decimal)model.PresentCount / model.TotalStudents * 100m, 1);
            model.LateRate = decimal.Round((decimal)model.LateCount / model.TotalStudents * 100m, 1);
            model.AbsentRate = decimal.Round((decimal)model.AbsentCount / model.TotalStudents * 100m, 1);
            model.UnmarkedRate = decimal.Round((decimal)model.UnmarkedCount / model.TotalStudents * 100m, 1);
        }

        return View(model);
    }
}
