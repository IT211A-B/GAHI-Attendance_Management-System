using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.TeacherHistory;
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
    public async Task<IActionResult> Index([FromQuery] int? scheduleId, [FromQuery] DateOnly? date)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var viewModel = new TeacherHistoryIndexViewModel
        {
            SelectedScheduleId = scheduleId,
            SelectedDate = selectedDate
        };

        List<TeacherScheduleDto> schedules;

        try
        {
            schedules = await _teacherHistoryService.GetTeacherSchedulesAsync(userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load teacher schedules for user {UserId}.", userId.Value);
            viewModel.ErrorMessage = "Unable to load your assigned schedules right now.";
            return View(viewModel);
        }

        viewModel.Schedules = schedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(schedule => new TeacherScheduleOptionViewModel
            {
                Id = schedule.Id,
                Label = $"{schedule.SectionName} | {schedule.SubjectName} | {schedule.DayName} {schedule.StartTime}-{schedule.EndTime}"
            })
            .ToList();

        if (scheduleId is null)
        {
            return View(viewModel);
        }

        ScheduleHistoryDto history;

        try
        {
            history = await _teacherHistoryService.GetScheduleHistoryAsync(scheduleId.Value, userId.Value, selectedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedule history for schedule {ScheduleId}.", scheduleId.Value);
            viewModel.ErrorMessage = "Unable to load schedule history right now.";
            return View(viewModel);
        }

        if (history is null)
        {
            viewModel.ErrorMessage = "Unable to load schedule history right now.";
            return View(viewModel);
        }

        viewModel.ScheduleDetails = new TeacherScheduleDetailsViewModel
        {
            SubjectName = history.Schedule.SubjectName,
            Section = history.Schedule.Section,
            Classroom = history.Schedule.Classroom,
            Day = history.Schedule.Day,
            StartTime = history.Schedule.StartTime,
            EndTime = history.Schedule.EndTime
        };

        viewModel.TotalStudents = history.Summary.TotalStudents;
        viewModel.PresentCount = history.Summary.PresentCount;
        viewModel.LateCount = history.Summary.LateCount;
        viewModel.AbsentCount = history.Summary.AbsentCount;
        viewModel.UnmarkedCount = history.Summary.UnmarkedCount;

        viewModel.Records = history.Records
            .OrderBy(r => r.StudentName)
            .Select(record => new TeacherAttendanceRecordViewModel
            {
                StudentId = record.StudentId,
                StudentName = string.IsNullOrWhiteSpace(record.StudentName) ? "-" : record.StudentName,
                TimeInText = record.TimeIn?.ToString("HH:mm") ?? "-",
                TimeOutText = record.TimeOut?.ToString("HH:mm") ?? "-",
                Remarks = string.IsNullOrWhiteSpace(record.Remarks) ? record.StatusLabel : record.Remarks
            })
            .ToList();

        return View(viewModel);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}