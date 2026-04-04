using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("teachers")]
public class TeacherManagementController : Controller
{
    private readonly ITeachersService _teachersService;

    public TeacherManagementController(ITeachersService teachersService)
    {
        _teachersService = teachersService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _teachersService.GetAllTeachersWithSectionsAsync();

        var viewModel = new TeachersIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load teachers right now.";
            return View(viewModel);
        }

        viewModel.Teachers = result.Data
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(teacher => new TeacherListItemViewModel
            {
                Id = teacher.Id,
                Name = $"{teacher.LastName}, {teacher.FirstName}",
                Email = teacher.Email,
                Department = teacher.Department,
                EmployeeNumber = teacher.EmployeeNumber,
                IsActive = teacher.IsActive,
                SectionsText = teacher.Sections.Count == 0
                    ? "-"
                    : string.Join(", ", teacher.Sections.Select(s => s.SectionName))
            })
            .ToList();

        return View(viewModel);
    }
}