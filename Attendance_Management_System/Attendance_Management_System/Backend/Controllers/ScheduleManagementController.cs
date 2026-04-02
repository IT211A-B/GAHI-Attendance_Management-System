using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("schedules")]
public class ScheduleManagementController : Controller
{
    private readonly ISchedulesService _schedulesService;

    public ScheduleManagementController(ISchedulesService schedulesService)
    {
        _schedulesService = schedulesService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return Challenge();
        }

        var result = await _schedulesService.GetSchedulesAsync(userId, role);
        var viewModel = new SchedulesIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load schedules right now.";
            return View(viewModel);
        }

        viewModel.Schedules = result.Data
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.SectionName)
            .Select(schedule => new ScheduleListItemViewModel
            {
                Id = schedule.Id,
                SectionName = schedule.SectionName,
                SubjectName = schedule.SubjectName,
                ClassroomName = schedule.ClassroomName,
                DayName = schedule.DayName,
                TimeRange = $"{schedule.StartTime} - {schedule.EndTime}",
                IsMine = schedule.IsMine
            })
            .ToList();

        return View(viewModel);
    }
}