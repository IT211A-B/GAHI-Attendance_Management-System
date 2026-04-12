using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Programs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("programs")]
public class ProgramsController : Controller
{
    private readonly ICoursesService _coursesService;

    public ProgramsController(ICoursesService coursesService)
    {
        _coursesService = coursesService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateProgramFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _coursesService.CreateCourseAsync(new CreateCourseRequest
        {
            Code = form.Code.Trim(),
            Name = form.Name.Trim(),
            Description = NormalizeOptional(form.Description)
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Code", result.Error?.Message ?? "Unable to create program right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["ProgramsSuccess"] = "Program created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateProgramFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["ProgramsError"] = "Please provide a valid code and name.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _coursesService.UpdateCourseAsync(id, new UpdateCourseRequest
        {
            Code = form.Code.Trim(),
            Name = form.Name.Trim(),
            Description = NormalizeOptional(form.Description)
        });

        if (!result.Success)
        {
            TempData["ProgramsError"] = result.Error?.Message ?? "Unable to update program right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ProgramsSuccess"] = "Program updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _coursesService.DeleteCourseAsync(id);

        if (!result.Success)
        {
            TempData["ProgramsError"] = result.Error?.Message ?? "Unable to delete program right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ProgramsSuccess"] = "Program deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProgramsIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _coursesService.GetAllCoursesAsync();
        var viewModel = new ProgramsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load programs right now.";
            return viewModel;
        }

        viewModel.Programs = result.Data
            .OrderBy(p => p.Code)
            .Select(program => new ProgramListItemViewModel
            {
                Id = program.Id,
                Code = program.Code,
                Name = program.Name,
                Description = string.IsNullOrWhiteSpace(program.Description) ? "-" : program.Description,
                CreatedAt = program.CreatedAt.ToString("yyyy-MM-dd")
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
