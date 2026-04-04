using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.TeacherHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "TeacherOnly")]
[Route("teacher-history")]
public class TeacherHistoryManagementController : Controller
{
    private readonly ITeacherHistoryService _teacherHistoryService;

    public TeacherHistoryManagementController(ITeacherHistoryService teacherHistoryService)
    {
        _teacherHistoryService = teacherHistoryService;
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

        var schedulesResult = await _teacherHistoryService.GetTeacherSchedulesAsync(userId.Value);

        if (!schedulesResult.Success || schedulesResult.Data is null)
        {
            viewModel.ErrorMessage = schedulesResult.Error?.Message ?? "Unable to load your assigned schedules right now.";
            return View(viewModel);
        }

        viewModel.Schedules = schedulesResult.Data
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

        var historyResult = await _teacherHistoryService.GetScheduleHistoryAsync(scheduleId.Value, userId.Value, selectedDate);
        if (!historyResult.Success || historyResult.Data is null)
        {
            viewModel.ErrorMessage = historyResult.Error?.Message ?? "Unable to load schedule history right now.";
            return View(viewModel);
        }

        viewModel.ScheduleDetails = new TeacherScheduleDetailsViewModel
        {
            SubjectName = historyResult.Data.Schedule.SubjectName,
            Section = historyResult.Data.Schedule.Section,
            Classroom = historyResult.Data.Schedule.Classroom,
            Day = historyResult.Data.Schedule.Day,
            StartTime = historyResult.Data.Schedule.StartTime,
            EndTime = historyResult.Data.Schedule.EndTime
        };

        viewModel.TotalStudents = historyResult.Data.Summary.TotalStudents;
        viewModel.PresentCount = historyResult.Data.Summary.PresentCount;
        viewModel.LateCount = historyResult.Data.Summary.LateCount;
        viewModel.AbsentCount = historyResult.Data.Summary.AbsentCount;

        viewModel.Records = historyResult.Data.Records
            .OrderBy(r => r.StudentName)
            .Select(record => new TeacherAttendanceRecordViewModel
            {
                StudentId = record.StudentId,
                StudentName = string.IsNullOrWhiteSpace(record.StudentName) ? "-" : record.StudentName,
                TimeInText = record.TimeIn?.ToString("HH:mm") ?? "-",
                TimeOutText = record.TimeOut?.ToString("HH:mm") ?? "-",
                Remarks = string.IsNullOrWhiteSpace(record.Remarks) ? "Present" : record.Remarks
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
