using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("students")]
[Route("student")]
public class StudentsManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly IStudentsService _studentsService;
    private readonly ITeachersService _teachersService;

    public StudentsManagementController(ISectionsService sectionsService, IStudentsService studentsService, ITeachersService teachersService)
    {
        _sectionsService = sectionsService;
        _studentsService = studentsService;
        _teachersService = teachersService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId)
    {
        if (!TryGetCurrentUserContext(out var userId, out var role))
        {
            return Challenge();
        }

        var isTeacher = string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase);
        var viewModel = new StudentsIndexViewModel
        {
            SelectedSectionId = sectionId,
            IsTeacher = isTeacher
        };

        var sectionsResult = isTeacher
            ? await _sectionsService.GetSectionsByTeacherUserIdAsync(userId)
            : await _sectionsService.GetAllSectionsAsync();

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

        if (isTeacher && !viewModel.Sections.Any())
        {
            viewModel.ErrorMessage = "You are not assigned to any section yet.";
            viewModel.SelectedSectionId = null;
            return View(viewModel);
        }

        if (sectionId is null)
        {
            return View(viewModel);
        }

        if (!viewModel.Sections.Any(section => section.Id == sectionId.Value))
        {
            viewModel.SelectedSectionId = null;
            viewModel.ErrorMessage = isTeacher
                ? "You can only view students in sections assigned to you."
                : "Selected section is not available.";
            return View(viewModel);
        }

        viewModel.IsCurrentTeacherAssignedToSelectedSection = isTeacher;

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

    [HttpPost("{id:int}/self-unassign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfUnassign(int id)
    {
        if (!TryGetCurrentUserContext(out var userId, out _))
        {
            return Challenge();
        }

        var teacherResult = await _teachersService.GetTeacherByUserIdAsync(userId);
        if (!teacherResult.Success || teacherResult.Data is null)
        {
            TempData["StudentsError"] = teacherResult.Error?.Message ?? "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = id });
        }

        // Self-unassign also removes schedules owned by this teacher in the section.
        var removeResult = await _sectionsService.RemoveTeacherFromSectionAsync(
            id,
            teacherResult.Data.Id,
            isAdmin: true,
            removeOwnedSchedules: true);
        if (!removeResult.Success)
        {
            TempData["StudentsError"] = removeResult.Error?.Message ?? "Unable to unassign from this section right now.";
            return RedirectToAction(nameof(Index), new { sectionId = id });
        }

        TempData["StudentsSuccess"] = "You are no longer assigned to this section and your schedules were removed.";
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetCurrentUserContext(out int userId, out string role)
    {
        userId = 0;
        role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out userId) || string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return true;
    }
}
