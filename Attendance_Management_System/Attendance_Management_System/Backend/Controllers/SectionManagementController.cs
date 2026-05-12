using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("sections")]
public class SectionManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly ISectionPageService _sectionPageService;
    private readonly ILogger<SectionManagementController> _logger;

    public SectionManagementController(
        ISectionsService sectionsService,
        ISectionPageService sectionPageService,
        ILogger<SectionManagementController> logger)
    {
        _sectionsService = sectionsService;
        _sectionPageService = sectionPageService;
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

        SectionManagementIndexViewModel viewModel;

        try
        {
            viewModel = await _sectionPageService.BuildSectionManagementIndexViewModelAsync(context.UserId, context.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section management index view.");
            viewModel = new SectionManagementIndexViewModel
            {
                ErrorMessage = "Unable to load sections right now."
            };
        }

        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateSectionFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await _sectionPageService.BuildSectionManagementIndexViewModelAsync(context.UserId, context.Role);
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        try
        {
            await _sectionsService.CreateSectionAsync(new CreateSectionRequest
            {
                Name = form.Name.Trim(),
                YearLevel = form.YearLevel,
                AcademicYearId = form.AcademicYearId,
                CourseId = form.CourseId,
                SubjectId = form.SubjectId,
                ClassroomId = form.ClassroomId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create section.");
            ModelState.AddModelError("CreateForm.Name", "Unable to create section right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["SectionsSuccess"] = "Section created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateSectionFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SectionsError"] = "Please provide valid section details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _sectionsService.UpdateSectionAsync(id, new UpdateSectionRequest
            {
                Name = form.Name.Trim(),
                YearLevel = form.YearLevel
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update section {SectionId}.", id);
            TempData["SectionsError"] = "Unable to update section right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Section updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _sectionsService.DeleteSectionAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete section {SectionId}.", id);
            TempData["SectionsError"] = "Unable to delete section right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Section deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/teachers/assign")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeacher(int id, [Bind(Prefix = "AssignForm")] AssignSectionTeacherFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SectionsError"] = "Please select a valid teacher.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _sectionsService.AssignTeacherToSectionAsync(id, new AssignTeacherRequest
            {
                TeacherId = form.TeacherId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign teacher {TeacherId} to section {SectionId}.", form.TeacherId, id);
            TempData["SectionsError"] = "Unable to assign teacher right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Teacher assigned successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/teachers/remove")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacher(int id, int teacherId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var isAdmin = role.IsRole(UserRole.Admin);

        try
        {
            await _sectionsService.RemoveTeacherFromSectionAsync(id, teacherId, isAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove teacher {TeacherId} from section {SectionId}.", teacherId, id);
            TempData["SectionsError"] = "Unable to remove teacher assignment right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Teacher removed from section successfully.";
        return RedirectToAction(nameof(Index));
    }

    private (bool IsValid, int UserId, string Role) GetUserContext()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return (false, 0, string.Empty);
        }

        return (true, userId, role);
    }
}