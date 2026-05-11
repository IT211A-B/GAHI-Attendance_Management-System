using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("subjects")]
public class SubjectsManagementController : Controller
{
    private readonly ISubjectsService _subjectsService;
    private readonly ILogger<SubjectsManagementController> _logger;

    public SubjectsManagementController(ISubjectsService subjectsService, ILogger<SubjectsManagementController> logger)
    {
        _subjectsService = subjectsService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SubjectsIndexViewModel viewModel;

        try
        {
            var subjects = await _subjectsService.GetAllSubjectsAsync();
            viewModel = new SubjectsIndexViewModel
            {
                Subjects = subjects
                    .OrderBy(s => s.Code)
                    .Select(s => new SubjectListItemViewModel
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Name = s.Name,
                        IsActive = s.IsActive
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load subjects.");
            viewModel = new SubjectsIndexViewModel
            {
                ErrorMessage = "Unable to load subjects right now."
            };
        }

        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateSubjectFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SubjectsError"] = "Please provide valid subject details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _subjectsService.CreateSubjectAsync(new CreateSubjectRequest
            {
                Code = form.Code.Trim(),
                Name = form.Name.Trim(),
                Department = form.Department?.Trim(),
                Units = form.Units
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subject.");
            TempData["SubjectsError"] = "Unable to create subject right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubjectsSuccess"] = "Subject created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateSubjectFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SubjectsError"] = "Please provide valid subject details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _subjectsService.UpdateSubjectAsync(id, new UpdateSubjectRequest
            {
                Code = form.Code.Trim(),
                Name = form.Name.Trim(),
                Department = form.Department?.Trim(),
                Units = form.Units
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subject {SubjectId}.", id);
            TempData["SubjectsError"] = "Unable to update subject right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubjectsSuccess"] = "Subject updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? replacementSubjectId)
    {
        try
        {
            await _subjectsService.DeleteSubjectAsync(id, replacementSubjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subject {SubjectId}.", id);
            TempData["SubjectsError"] = "Unable to delete subject right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubjectsSuccess"] = "Subject deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}