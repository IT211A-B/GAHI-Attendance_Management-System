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
        var result = await _sectionsService.GetAllSectionsAsync();

        var viewModel = new SectionsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load sections right now.";
            return View(viewModel);
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

        return View(viewModel);
    }
}