using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("students")]
public class StudentsManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly IStudentsService _studentsService;

    public StudentsManagementController(ISectionsService sectionsService, IStudentsService studentsService)
    {
        _sectionsService = sectionsService;
        _studentsService = studentsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId)
    {
        var sectionsResult = await _sectionsService.GetAllSectionsAsync();
        var viewModel = new StudentsIndexViewModel { SelectedSectionId = sectionId };

        if (!sectionsResult.Success || sectionsResult.Data is null)
        {
            viewModel.ErrorMessage = sectionsResult.Error?.Message ?? "Unable to load sections right now.";
            return View(viewModel);
        }

        viewModel.Sections = sectionsResult.Data
            .OrderBy(s => s.Name)
            .Select(section => new StudentsSectionOptionViewModel
            {
                Id = section.Id,
                Name = section.Name
            })
            .ToList();

        if (sectionId is null)
        {
            return View(viewModel);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return Challenge();
        }

        var studentsResult = await _studentsService.GetStudentsBySectionAsync(sectionId.Value, userId, role);

        if (!studentsResult.Success || studentsResult.Data is null)
        {
            viewModel.ErrorMessage = studentsResult.Error?.Message ?? "Unable to load students for the selected section.";
            return View(viewModel);
        }

        viewModel.Students = studentsResult.Data
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(student => new StudentListItemViewModel
            {
                Id = student.Id,
                StudentNumber = student.StudentNumber,
                FullName = string.Join(" ", new[] { student.FirstName, student.MiddleName, student.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                YearLevel = student.YearLevel,
                CourseText = string.IsNullOrWhiteSpace(student.CourseCode) && string.IsNullOrWhiteSpace(student.CourseName)
                    ? "-"
                    : $"{student.CourseCode} {student.CourseName}".Trim(),
                SectionName = string.IsNullOrWhiteSpace(student.SectionName) ? "-" : student.SectionName
            })
            .ToList();

        return View(viewModel);
    }
}
