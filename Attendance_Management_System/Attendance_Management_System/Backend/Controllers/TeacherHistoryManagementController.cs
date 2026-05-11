using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "TeacherOnly")]
[Route("teacher-history")]
public class TeacherHistoryManagementController : Controller
{
    private readonly ITeacherHistoryService _teacherHistoryService;
    private readonly ILogger<TeacherHistoryManagementController> _logger;

    public TeacherHistoryManagementController(ITeacherHistoryService teacherHistoryService, ILogger<TeacherHistoryManagementController> logger)
    {
        _teacherHistoryService = teacherHistoryService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? scheduleId, [FromQuery] DateOnly? selectedDate)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Challenge();

        var actualDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        var viewModel = new TeacherHistoryIndexViewModel
        {
            SelectedScheduleId = scheduleId,
            SelectedDate = actualDate
        };

        List<ScheduleDto> teacherSchedules;

        try
        {
            teacherSchedules = await _teacherHistoryService.GetTeacherSchedulesAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load teacher schedules for user {UserId}.", userId);
            viewModel.ErrorMessage = "Unable to load your schedules right now.";
            return View(viewModel);
        }

        viewModel.Schedules = teacherSchedules
            .Select(schedule => new TeacherScheduleOptionViewModel
            {
                Id = schedule.Id,
                SectionName = schedule.SectionName,
                SubjectName = schedule.SubjectName,
                DayName = schedule.DayName,
                TimeRange = $"{schedule.StartTime} - {schedule.EndTime}"
            })
            .ToList();

        if (!scheduleId.HasValue || !viewModel.Schedules.Any(s => s.Id == scheduleId.Value))
        {
            return View(viewModel);
        }

        viewModel.SelectedScheduleId = scheduleId;

        List<ScheduleHistoryDto> history;

        try
        {
            history = await _teacherHistoryService.GetScheduleHistoryAsync(scheduleId.Value, userId, actualDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedule history for schedule {ScheduleId}.", scheduleId.Value);
            viewModel.ErrorMessage = "Unable to load schedule history right now.";
            return View(viewModel);
        }

        viewModel.History = history
            .OrderByDescending(h => h.Date)
            .ThenBy(h => h.StudentName)
            .Select(entry => new ScheduleHistoryItemViewModel
            {
                Date = entry.Date,
                StudentName = entry.StudentName,
                AttendanceStatus = entry.AttendanceStatus,
                IsPresent = entry.IsPresent,
                IsLate = entry.IsLate,
                IsAbsent = entry.IsAbsent,
                IsUnmarked = entry.IsUnmarked,
                TimeIn = entry.TimeIn,
                Remarks = entry.Remarks
            })
            .ToList();

        return View(viewModel);
    }
}