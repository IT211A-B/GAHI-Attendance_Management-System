using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("sections")]
public class SectionManagementController : Controller
{
    private readonly ISectionsService _sectionsService;

    public SectionManagementController(ISectionsService sectionsService)
    {
        _sectionsService = sectionsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateSectionFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _sectionsService.CreateSectionAsync(new CreateSectionRequest
        {
            Name = form.Name.Trim(),
            YearLevel = form.YearLevel,
            AcademicYearId = form.AcademicYearId,
            CourseId = form.CourseId,
            SubjectId = form.SubjectId,
            ClassroomId = form.ClassroomId
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Name", result.Error?.Message ?? "Unable to create section right now.");
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

        var result = await _sectionsService.UpdateSectionAsync(id, new UpdateSectionRequest
        {
            Name = form.Name.Trim(),
            YearLevel = form.YearLevel
        });

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to update section right now.";
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
        var result = await _sectionsService.DeleteSectionAsync(id);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to delete section right now.";
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
            TempData["SectionsError"] = "Please provide a valid teacher ID.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _sectionsService.AssignTeacherToSectionAsync(id, new AssignTeacherRequest
        {
            TeacherId = form.TeacherId
        });

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to assign teacher right now.";
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
        var isAdmin = role == "admin";

        var result = await _sectionsService.RemoveTeacherFromSectionAsync(id, teacherId, isAdmin);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to remove teacher assignment right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Teacher removed from section successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SectionsIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _sectionsService.GetAllSectionsAsync();

        var viewModel = new SectionsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load sections right now.";
            return viewModel;
        }

        viewModel.Sections = result.Data
            .OrderBy(s => s.Name)
            .Select(section => new SectionListItemViewModel
            {
                Id = section.Id,
                Name = section.Name,
                YearLevel = section.YearLevel,
                CourseName = string.IsNullOrWhiteSpace(section.CourseName) ? "-" : section.CourseName,
                SubjectName = string.IsNullOrWhiteSpace(section.SubjectName) ? "-" : section.SubjectName,
                ClassroomName = string.IsNullOrWhiteSpace(section.ClassroomName) ? "-" : section.ClassroomName,
                CurrentEnrollmentCount = section.CurrentEnrollmentCount
            })
            .ToList();

        return viewModel;
    }
}