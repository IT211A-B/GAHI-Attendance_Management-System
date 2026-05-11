using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("reports")]
public class ReportsController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly ISchedulesService _schedulesService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ISectionsService sectionsService,
        ISchedulesService schedulesService,
        IAttendanceService attendanceService,
        ILogger<ReportsController> logger)
    {
        _sectionsService = sectionsService;
        _schedulesService = schedulesService;
        _attendanceService = attendanceService;
        _logger = logger;
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

        List<ScheduleDto> schedules;

        try
        {
            schedules = await _schedulesService.GetSchedulesAsync(userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedules for reports view.");
            model.ErrorMessage = "Unable to load schedules.";
            return View(model);
        }

        var accessibleSchedules = schedules
            .Where(schedule => !role.IsRole(UserRole.Teacher) || schedule.IsMine)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .ToList();

        if (role.IsRole(UserRole.Admin))
        {
            List<SectionDto> sections;

            try
            {
                sections = await _sectionsService.GetAllSectionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sections for reports view.");
                model.ErrorMessage = "Unable to load sections.";
                return View(model);
            }

            model.Sections = sections
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

        AttendanceSummaryDto summary;

        try
        {
            summary = await _attendanceService.GetSectionAttendanceAsync(
                sectionId.Value,
                selectedDate,
                scheduleId.Value,
                userId,
                role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance summary for reports view.");
            model.ErrorMessage = "Unable to load report data.";
            return View(model);
        }

        model.TotalStudents = summary.TotalStudents;
        model.PresentCount = summary.PresentCount;
        model.LateCount = summary.LateCount;
        model.AbsentCount = summary.AbsentCount;
        model.UnmarkedCount = summary.UnmarkedCount;

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