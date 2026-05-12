using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("timetable")]
public class TimetableController : Controller
{
    private readonly ISectionPageService _sectionPageService;
    private readonly ISectionsService _sectionsService;
    private readonly ISchedulesService _schedulesService;
    private readonly ILogger<TimetableController> _logger;

    public TimetableController(
        ISectionPageService sectionPageService,
        ISectionsService sectionsService,
        ISchedulesService schedulesService,
        ILogger<TimetableController> logger)
    {
        _sectionPageService = sectionPageService;
        _sectionsService = sectionsService;
        _schedulesService = schedulesService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        TimetableIndexViewModel viewModel;

        try
        {
            viewModel = await _sectionPageService.BuildTimetableIndexViewModelAsync(context.UserId, context.Role, sectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load timetable index view.");
            viewModel = new TimetableIndexViewModel
            {
                ErrorMessage = "Unable to load timetable data right now."
            };
        }

        return View(viewModel);
    }

    [HttpPost("{id:int}/teachers/self-assign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfAssignTeacher(int id, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var teacherContext = await _sectionPageService.BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContext.Success || !teacherContext.Context.TeacherId.HasValue)
        {
            TempData["TimetableError"] = teacherContext.Error ?? "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        var teacherId = teacherContext.Context.TeacherId.Value;

        List<SectionTeacherDto> sectionTeachers;

        try
        {
            sectionTeachers = await _sectionsService.GetSectionTeachersAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section teachers for section {SectionId}.", id);
            TempData["TimetableError"] = "Unable to load section teacher assignments right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        if (sectionTeachers.Any(assignment => assignment.TeacherId == teacherId))
        {
            TempData["TimetableSuccess"] = "You are already assigned to this section.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        try
        {
            await _sectionsService.AssignTeacherToSectionAsync(id, new AssignTeacherRequest
            {
                TeacherId = teacherId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to self-assign teacher {TeacherId} to section {SectionId}.", teacherId, id);
            TempData["TimetableError"] = "Unable to self-assign to this section right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        TempData["TimetableSuccess"] = "You are now assigned to this section.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
    }

    [HttpPost("{id:int}/teachers/self-unassign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfUnassignTeacher(int id, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var teacherContext = await _sectionPageService.BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContext.Success || !teacherContext.Context.TeacherId.HasValue)
        {
            TempData["TimetableError"] = teacherContext.Error ?? "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        var teacherId = teacherContext.Context.TeacherId.Value;

        List<SectionTeacherDto> sectionTeachers;

        try
        {
            sectionTeachers = await _sectionsService.GetSectionTeachersAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section teachers for section {SectionId}.", id);
            TempData["TimetableError"] = "Unable to load section teacher assignments right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        if (!sectionTeachers.Any(assignment => assignment.TeacherId == teacherId))
        {
            TempData["TimetableSuccess"] = "You are not assigned to this section.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        try
        {
            await _sectionsService.RemoveTeacherFromSectionAsync(
                id,
                teacherId,
                isAdmin: true,
                removeOwnedSchedules: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to self-unassign teacher {TeacherId} from section {SectionId}.", teacherId, id);
            TempData["TimetableError"] = "Unable to unassign from this section right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        TempData["TimetableSuccess"] = "You are no longer assigned to this section and your schedules were removed.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
    }

    [HttpPost("slots/add")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableSlot(
        int sectionId,
        int subjectId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["TimetableError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var subjectSelectionValidation = await _sectionPageService.ValidateSubjectSelectionForSectionAsync(sectionId, subjectId);
        if (!subjectSelectionValidation.IsValid)
        {
            TempData["TimetableError"] = subjectSelectionValidation.ErrorMessage;
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        try
        {
            await _schedulesService.CreateScheduleAsync(new CreateScheduleRequest
            {
                SectionId = sectionId,
                SubjectId = subjectId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
            }, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add timetable slot for section {SectionId}.", sectionId);
            TempData["TimetableError"] = "Unable to add the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        TempData["TimetableSuccess"] = "Timetable slot added successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
    }

    [HttpPost("slots/add-range")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableSlotRange(
        int sectionId,
        int subjectId,
        TimeOnly startTime,
        TimeOnly endTime,
        [FromForm] List<int>? selectedDays,
        int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["TimetableError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var normalizedDays = (selectedDays ?? [])
            .Where(day => day >= 0 && day <= 6)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        if (normalizedDays.Count == 0)
        {
            TempData["TimetableError"] = "Select at least one weekday.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var subjectSelectionValidation = await _sectionPageService.ValidateSubjectSelectionForSectionAsync(sectionId, subjectId);
        if (!subjectSelectionValidation.IsValid)
        {
            TempData["TimetableError"] = subjectSelectionValidation.ErrorMessage;
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        List<ScheduleDto> createdSlots;

        try
        {
            createdSlots = await _schedulesService.CreateScheduleRangeAsync(new CreateScheduleRangeRequest
            {
                SectionId = sectionId,
                SubjectId = subjectId,
                DaysOfWeek = normalizedDays,
                StartTime = startTime,
                EndTime = endTime,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
            }, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add timetable slot range for section {SectionId}.", sectionId);
            TempData["TimetableError"] = "Unable to add timetable slots right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var createdCount = createdSlots.Count;
        TempData["TimetableSuccess"] = createdCount == 1
            ? "Timetable slot added successfully."
            : $"Timetable slots added successfully ({createdCount} days).";

        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
    }

    [HttpPost("slots/{scheduleId:int}/update")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTimetableSlot(
        int scheduleId,
        int subjectId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["TimetableError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        if (subjectId <= 0)
        {
            TempData["TimetableError"] = "Select a subject before saving the timetable slot.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        try
        {
            await _schedulesService.UpdateScheduleAsync(scheduleId, new UpdateScheduleRequest
            {
                SubjectId = subjectId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            }, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update timetable slot {ScheduleId}.", scheduleId);
            TempData["TimetableError"] = "Unable to update the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        TempData["TimetableSuccess"] = "Timetable slot updated successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
    }

    [HttpPost("slots/{scheduleId:int}/delete")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTimetableSlot(int scheduleId, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        try
        {
            await _schedulesService.DeleteScheduleAsync(scheduleId, context.UserId, isAdmin: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete timetable slot {ScheduleId}.", scheduleId);
            TempData["TimetableError"] = "Unable to delete the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        TempData["TimetableSuccess"] = "Timetable slot deleted successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
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