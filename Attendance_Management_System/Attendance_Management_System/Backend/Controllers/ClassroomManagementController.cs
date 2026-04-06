using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Classrooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("departments")]
[Route("classrooms")]
public class ClassroomManagementController : Controller
{
    private readonly IClassroomsService _classroomsService;

    public ClassroomManagementController(IClassroomsService classroomsService)
    {
        _classroomsService = classroomsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateClassroomFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _classroomsService.CreateClassroomAsync(new CreateClassroomRequest
        {
            Name = form.Name.Trim(),
            Description = NormalizeOptional(form.Description)
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Name", result.Error?.Message ?? "Unable to create classroom right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["ClassroomsSuccess"] = "Classroom created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateClassroomFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["ClassroomsError"] = "Please provide a valid classroom name.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _classroomsService.UpdateClassroomAsync(id, new UpdateClassroomRequest
        {
            Name = form.Name.Trim(),
            Description = NormalizeOptional(form.Description)
        });

        if (!result.Success)
        {
            TempData["ClassroomsError"] = result.Error?.Message ?? "Unable to update classroom right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ClassroomsSuccess"] = "Classroom updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _classroomsService.DeleteClassroomAsync(id);

        if (!result.Success)
        {
            TempData["ClassroomsError"] = result.Error?.Message ?? "Unable to delete classroom right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ClassroomsSuccess"] = "Classroom deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ClassroomsIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _classroomsService.GetAllClassroomsAsync();

        var viewModel = new ClassroomsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load classrooms right now.";
            return viewModel;
        }

        viewModel.Classrooms = result.Data
            .OrderBy(c => c.Name)
            .Select(classroom => new ClassroomListItemViewModel
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = string.IsNullOrWhiteSpace(classroom.Description) ? "-" : classroom.Description
            })
            .ToList();

        return viewModel;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}