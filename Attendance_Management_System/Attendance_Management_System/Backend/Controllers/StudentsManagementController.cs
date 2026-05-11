using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("students")]
[Route("student")]
public class StudentsManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly IStudentsService _studentsService;
    private readonly ITeachersService _teachersService;
    private readonly ILogger<StudentsManagementController> _logger;

    public StudentsManagementController(
        ISectionsService sectionsService,
        IStudentsService studentsService,
        ITeachersService teachersService,
        ILogger<StudentsManagementController> logger)
    {
        _sectionsService = sectionsService;
        _studentsService = studentsService;
        _teachersService = teachersService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!int.TryParse(userIdClaim, out var userId))
            return Challenge();

        var isTeacher = role.IsRole(UserRole.Teacher);
        var viewModel = new StudentsIndexViewModel
        {
            IsTeacher = isTeacher
        };

        List<SectionDto> sections;

        try
        {
            sections = isTeacher
                ? await _sectionsService.GetSectionsByTeacherUserIdAsync(userId)
                : await _sectionsService.GetAllSectionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sections for students view.");
            viewModel.ErrorMessage = "Unable to load sections right now.";
            return View(viewModel);
        }

        viewModel.IsCurrentTeacherAssignedToSelectedSection = isTeacher;
        viewModel.Sections = sections
            .OrderBy(s => s.Name)
            .Select(section => new StudentsSectionOptionViewModel
            {
                Id = section.Id,
                Name = section.Name
            })
            .ToList();

        if (!sectionId.HasValue)
            return View(viewModel);

        viewModel.SelectedSectionId = sectionId;
        var selectedSection = sections.FirstOrDefault(s => s.Id == sectionId.Value);
        if (selectedSection is null)
        {
            viewModel.ErrorMessage = "The selected section was not found.";
            return View(viewModel);
        }

        List<StudentBasicProfileDto> students;

        try
        {
            students = await _studentsService.GetStudentsBySectionAsync(sectionId.Value, userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load students for section {SectionId}.", sectionId.Value);
            viewModel.ErrorMessage = "Unable to load students for the selected section.";
            return View(viewModel);
        }

        viewModel.Students = students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(student => new StudentListItemViewModel
            {
                Id = student.Id,
                StudentNumber = student.StudentNumber ?? "-",
                FullName = student.FullName ?? "-",
                Email = student.Email ?? "-",
                Status = student.Status
            })
            .ToList();

        return View(viewModel);
    }

    [HttpPost("{id:int}/self-unassign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfUnassign(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Challenge();

        TeacherDto? teacher;

        try
        {
            teacher = await _teachersService.GetTeacherByUserIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve teacher profile for user {UserId}.", userId);
            TempData["StudentsError"] = "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = id });
        }

        if (teacher is null)
        {
            TempData["StudentsError"] = "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = id });
        }

        try
        {
            await _sectionsService.RemoveTeacherFromSectionAsync(id, teacher.Id, isAdmin: true, removeOwnedSchedules: true);
            TempData["StudentsSuccess"] = "You have been unassigned from this section.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to self-unassign teacher {TeacherId} from section {SectionId}.", teacher.Id, id);
            TempData["StudentsError"] = "Unable to unassign from this section right now.";
        }

        return RedirectToAction(nameof(Index), new { sectionId = id });
    }
}