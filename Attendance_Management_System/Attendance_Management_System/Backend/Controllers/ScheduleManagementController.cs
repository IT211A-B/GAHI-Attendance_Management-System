using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
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

        var result = await _schedulesService.CreateScheduleAsync(new CreateScheduleRequest
        {
            SectionId = form.SectionId,
            SubjectId = form.SubjectId,
            DayOfWeek = form.DayOfWeek,
            StartTime = form.StartTime,
            EndTime = form.EndTime,
            EffectiveFrom = form.EffectiveFrom,
            EffectiveTo = form.EffectiveTo
        }, context.UserId);

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.SectionId", result.Error?.Message ?? "Unable to create schedule right now.");
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

        var result = await _schedulesService.UpdateScheduleAsync(id, new UpdateScheduleRequest
        {
            SubjectId = form.SubjectId,
            DayOfWeek = form.DayOfWeek,
            StartTime = form.StartTime,
            EndTime = form.EndTime,
            EffectiveFrom = form.EffectiveFrom,
            EffectiveTo = form.EffectiveTo
        }, context.UserId);

        if (!result.Success)
        {
            TempData["SchedulesError"] = result.Error?.Message ?? "Unable to update schedule right now.";
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

        var result = await _schedulesService.DeleteScheduleAsync(id, context.UserId, context.IsAdmin);

        if (!result.Success)
        {
            TempData["SchedulesError"] = result.Error?.Message ?? "Unable to delete schedule right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SchedulesSuccess"] = "Schedule deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SchedulesIndexViewModel> BuildIndexViewModelAsync(int userId, string role)
    {
        var result = await _schedulesService.GetSchedulesAsync(userId, role);
        var viewModel = new SchedulesIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load schedules right now.";
            return viewModel;
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

        return (true, userId, role, role == "admin");
    }
}