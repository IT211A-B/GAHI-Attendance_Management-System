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

        try
        {
            await _classroomsService.CreateClassroomAsync(new CreateClassroomRequest
            {
                Name = form.Name.Trim(),
                Description = NormalizeOptional(form.Description)
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("CreateForm.Name", string.IsNullOrWhiteSpace(ex.Message) ? "Unable to create classroom right now." : ex.Message);
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

        try
        {
            await _classroomsService.UpdateClassroomAsync(id, new UpdateClassroomRequest
            {
                Name = form.Name.Trim(),
                Description = NormalizeOptional(form.Description)
            });
        }
        catch (Exception ex)
        {
            TempData["ClassroomsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to update classroom right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ClassroomsSuccess"] = "Classroom updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _classroomsService.DeleteClassroomAsync(id);
        }
        catch (Exception ex)
        {
            TempData["ClassroomsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to delete classroom right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ClassroomsSuccess"] = "Classroom deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ClassroomsIndexViewModel> BuildIndexViewModelAsync()
    {
        var viewModel = new ClassroomsIndexViewModel();

        try
        {
            var classrooms = await _classroomsService.GetAllClassroomsAsync();

            viewModel.Classrooms = classrooms
                .OrderBy(c => c.Name)
                .Select(classroom => new ClassroomListItemViewModel
                {
                    Id = classroom.Id,
                    Name = classroom.Name,
                    Description = string.IsNullOrWhiteSpace(classroom.Description) ? "-" : classroom.Description
                })
                .ToList();
        }
        catch (Exception ex)
        {
            viewModel.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to load classrooms right now." : ex.Message;
        }

        return viewModel;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}