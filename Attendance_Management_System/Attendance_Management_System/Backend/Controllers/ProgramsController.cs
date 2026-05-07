using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
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

        try
        {
            await _coursesService.CreateCourseAsync(new CreateCourseRequest
            {
                Code = form.Code.Trim(),
                Name = form.Name.Trim(),
                EducationLevel = form.EducationLevel,
                Description = NormalizeOptional(form.Description)
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("CreateForm.Code", string.IsNullOrWhiteSpace(ex.Message) ? "Unable to create program right now." : ex.Message);
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

        try
        {
            await _coursesService.UpdateCourseAsync(id, new UpdateCourseRequest
            {
                Code = form.Code.Trim(),
                Name = form.Name.Trim(),
                EducationLevel = form.EducationLevel,
                Description = NormalizeOptional(form.Description)
            });
        }
        catch (Exception ex)
        {
            TempData["ProgramsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to update program right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ProgramsSuccess"] = "Program updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _coursesService.DeleteCourseAsync(id);
        }
        catch (Exception ex)
        {
            TempData["ProgramsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to delete program right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ProgramsSuccess"] = "Program deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProgramsIndexViewModel> BuildIndexViewModelAsync()
    {
        var viewModel = new ProgramsIndexViewModel();

        try
        {
            var programs = await _coursesService.GetAllCoursesAsync();

            viewModel.Programs = programs
                .OrderBy(p => p.Code)
                .Select(program => new ProgramListItemViewModel
                {
                    Id = program.Id,
                    Code = program.Code,
                    Name = program.Name,
                    EducationLevel = program.EducationLevel,
                    EducationLevelLabel = EducationLevelPolicy.ToDisplayLabel(program.EducationLevel),
                    Description = string.IsNullOrWhiteSpace(program.Description) ? "-" : program.Description,
                    CreatedAt = program.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToList();
        }
        catch (Exception ex)
        {
            viewModel.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to load programs right now." : ex.Message;
            viewModel.EducationLevels = Enum
                .GetValues<EducationLevel>()
                .Select(level => new ProgramEducationLevelOptionViewModel
                {
                    Value = level,
                    Label = EducationLevelPolicy.ToDisplayLabel(level)
                })
                .ToList();

            return viewModel;
        }

        viewModel.EducationLevels = Enum
            .GetValues<EducationLevel>()
            .Select(level => new ProgramEducationLevelOptionViewModel
            {
                Value = level,
                Label = EducationLevelPolicy.ToDisplayLabel(level)
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
