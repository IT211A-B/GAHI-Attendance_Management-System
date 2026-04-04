using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("subjects")]
public class SubjectsManagementController : Controller
{
    private readonly ISubjectsService _subjectsService;

    public SubjectsManagementController(ISubjectsService subjectsService)
    {
        _subjectsService = subjectsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateSubjectFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _subjectsService.CreateSubjectAsync(new CreateSubjectRequest
        {
            Name = form.Name.Trim(),
            Code = form.Code.Trim(),
            CourseId = form.CourseId,
            Units = form.Units
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Name", result.Error?.Message ?? "Unable to create subject right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["SubjectsSuccess"] = "Subject created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateSubjectFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SubjectsError"] = "Please provide valid subject details.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _subjectsService.UpdateSubjectAsync(id, new UpdateSubjectRequest
        {
            Name = form.Name.Trim(),
            Code = form.Code.Trim(),
            CourseId = form.CourseId,
            Units = form.Units
        });

        if (!result.Success)
        {
            TempData["SubjectsError"] = result.Error?.Message ?? "Unable to update subject right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubjectsSuccess"] = "Subject updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _subjectsService.DeleteSubjectAsync(id);

        if (!result.Success)
        {
            TempData["SubjectsError"] = result.Error?.Message ?? "Unable to delete subject right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubjectsSuccess"] = "Subject deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SubjectsIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _subjectsService.GetAllSubjectsAsync();

        var viewModel = new SubjectsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load subjects right now.";
            return viewModel;
        }

        viewModel.Subjects = result.Data
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Code)
            .Select(subject => new SubjectListItemViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                CourseId = subject.CourseId,
                CourseName = string.IsNullOrWhiteSpace(subject.CourseName) ? "-" : subject.CourseName,
                Units = subject.Units
            })
            .ToList();

        return viewModel;
    }
}
