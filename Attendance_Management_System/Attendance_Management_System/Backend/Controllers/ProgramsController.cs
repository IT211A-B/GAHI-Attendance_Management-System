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
        var result = await _coursesService.GetAllCoursesAsync();
        var viewModel = new ProgramsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load programs right now.";
            return View(viewModel);
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

        return View(viewModel);
    }
}
