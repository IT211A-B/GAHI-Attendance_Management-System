using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("subjects")]
public class SubjectsManagementController : Controller
{
    private readonly ISubjectsService _subjectsService;
    private readonly ICoursesService _coursesService;
    private readonly ILogger<SubjectsManagementController> _logger;

    public SubjectsManagementController(ISubjectsService subjectsService, ICoursesService coursesService, ILogger<SubjectsManagementController> logger)
    {
        _subjectsService = subjectsService;
        _coursesService = coursesService;
        _logger = logger;
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

        try
        {
            await _subjectsService.CreateSubjectAsync(new CreateSubjectRequest
            {
                Name = form.Name.Trim(),
                Code = form.Code.Trim(),
                CourseId = form.CourseId,
                Units = form.Units
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subject.");
            ModelState.AddModelError("CreateForm.CourseId", "Unable to create subject right now.");
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

        try
        {
            await _subjectsService.UpdateSubjectAsync(id, new UpdateSubjectRequest
            {
                Name = form.Name.Trim(),
                Code = form.Code.Trim(),
                CourseId = form.CourseId,
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
    [Authorize(Policy = "AdminOrTeacher")]
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

    private async Task<SubjectsIndexViewModel> BuildIndexViewModelAsync()
    {
        var viewModel = new SubjectsIndexViewModel();

        List<CourseDto> courses;

        try
        {
            courses = await _coursesService.GetAllCoursesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load courses for subjects view.");
            viewModel.ErrorMessage = "Unable to load courses right now.";
            return viewModel;
        }

        viewModel.Courses = courses
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

        List<SubjectDto> subjects;

        try
        {
            subjects = await _subjectsService.GetAllSubjectsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load subjects.");
            viewModel.ErrorMessage = "Unable to load subjects right now.";
            return viewModel;
        }

        viewModel.Subjects = subjects
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