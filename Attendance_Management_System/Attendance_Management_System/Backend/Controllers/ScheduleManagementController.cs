using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("schedules")]
public class ScheduleManagementController : Controller
{
    private readonly ISchedulesService _schedulesService;
    private readonly ILogger<ScheduleManagementController> _logger;

    public ScheduleManagementController(ISchedulesService schedulesService, ILogger<ScheduleManagementController> logger)
    {
        _schedulesService = schedulesService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(context.UserId, context.Role);
        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateScheduleFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(context.UserId, context.Role);
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        try
        {
            await _schedulesService.CreateScheduleAsync(new CreateScheduleRequest
            {
                SectionId = form.SectionId,
                SubjectId = form.SubjectId,
                DayOfWeek = form.DayOfWeek,
                StartTime = form.StartTime,
                EndTime = form.EndTime,
                EffectiveFrom = form.EffectiveFrom,
                EffectiveTo = form.EffectiveTo
            }, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create schedule for user {UserId}.", context.UserId);
            ModelState.AddModelError("CreateForm.SectionId", "Unable to create schedule right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["SchedulesSuccess"] = "Schedule created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateScheduleFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["SchedulesError"] = "Please provide valid schedule details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _schedulesService.UpdateScheduleAsync(id, new UpdateScheduleRequest
            {
                SubjectId = form.SubjectId,
                DayOfWeek = form.DayOfWeek,
                StartTime = form.StartTime,
                EndTime = form.EndTime,
                EffectiveFrom = form.EffectiveFrom,
                EffectiveTo = form.EffectiveTo
            }, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update schedule {ScheduleId}.", id);
            TempData["SchedulesError"] = "Unable to update schedule right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SchedulesSuccess"] = "Schedule updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        try
        {
            await _schedulesService.DeleteScheduleAsync(id, context.UserId, context.IsAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schedule {ScheduleId}.", id);
            TempData["SchedulesError"] = "Unable to delete schedule right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SchedulesSuccess"] = "Schedule deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SchedulesIndexViewModel> BuildIndexViewModelAsync(int userId, string role)
    {
        List<ScheduleDto> schedules;

        try
        {
            schedules = await _schedulesService.GetSchedulesAsync(userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedules for user {UserId}.", userId);
            var errorViewModel = new SchedulesIndexViewModel();
            errorViewModel.ErrorMessage = "Unable to load schedules right now.";
            return errorViewModel;
        }

        var viewModel = new SchedulesIndexViewModel();
        viewModel.Schedules = schedules
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

        return viewModel;
    }

    private (bool IsValid, int UserId, string Role, bool IsAdmin) GetUserContext()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return (false, 0, string.Empty, false);
        }

        return (true, userId, role, role.IsRole(UserRole.Admin));
    }
}