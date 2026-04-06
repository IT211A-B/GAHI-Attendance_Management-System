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
    private readonly ICoursesService _coursesService;

    public SubjectsManagementController(ISubjectsService subjectsService, ICoursesService coursesService)
    {
        _subjectsService = subjectsService;
        _coursesService = coursesService;
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
            ModelState.AddModelError("CreateForm.CourseId", result.Error?.Message ?? "Unable to create subject right now.");
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
    public async Task<IActionResult> Delete(int id, int? replacementSubjectId)
    {
        var result = await _subjectsService.DeleteSubjectAsync(id, replacementSubjectId);

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
        var viewModel = new SubjectsIndexViewModel();
        var coursesResult = await _coursesService.GetAllCoursesAsync();

        if (!coursesResult.Success || coursesResult.Data is null)
        {
            viewModel.ErrorMessage = coursesResult.Error?.Message ?? "Unable to load courses right now.";
            return viewModel;
        }

        viewModel.Courses = coursesResult.Data
            .OrderBy(course => course.Name)
            .Select(course => new SubjectCourseOptionViewModel
            {
                Id = course.Id,
                Label = BuildCourseOptionLabel(course.Code, course.Name)
            })
            .ToList();

        var courseLabelsById = viewModel.Courses
            .GroupBy(course => course.Id)
            .ToDictionary(group => group.Key, group => group.First().Label);

        var result = await _subjectsService.GetAllSubjectsAsync();

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
                CourseLabel = courseLabelsById.TryGetValue(subject.CourseId, out var label)
                    ? label
                    : string.IsNullOrWhiteSpace(subject.CourseName) ? "-" : subject.CourseName,
                Units = subject.Units
            })
            .ToList();

        return viewModel;
    }

    private static string BuildCourseOptionLabel(string? code, string? name)
    {
        var courseName = string.IsNullOrWhiteSpace(name) ? "Unnamed course" : name.Trim();
        return string.IsNullOrWhiteSpace(code)
            ? courseName
            : $"{code.Trim()} - {courseName}";
    }
}
