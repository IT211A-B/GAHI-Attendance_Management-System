using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("teachers")]
public class TeacherManagementController : Controller
{
    private readonly ITeachersService _teachersService;
    private readonly ILogger<TeacherManagementController> _logger;

    public TeacherManagementController(ITeachersService teachersService, ILogger<TeacherManagementController> logger)
    {
        _teachersService = teachersService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        TeacherIndexViewModel viewModel;

        try
        {
            var teachers = await _teachersService.GetAllTeachersWithSectionsAsync();
            viewModel = new TeacherIndexViewModel
            {
                Teachers = teachers
                    .Select(t => new TeacherListItemViewModel
                    {
                        Id = t.Id,
                        FirstName = t.FirstName,
                        MiddleName = t.MiddleName,
                        LastName = t.LastName,
                        Email = t.Email,
                        EmployeeNumber = t.EmployeeNumber,
                        IsActive = t.IsActive,
                        SectionNames = t.SectionNames ?? []
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load teachers.");
            viewModel = new TeacherIndexViewModel
            {
                ErrorMessage = "Unable to load teachers right now."
            };
        }

        return View(viewModel);
    }

    [HttpPost("{id:int}/toggle-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool activate)
    {
        try
        {
            if (activate)
                await _teachersService.ActivateTeacherAsync(id);
            else
                await _teachersService.DeactivateTeacherAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle status for teacher {TeacherId}.", id);
            TempData["TeachersError"] = $"Unable to {(activate ? "activate" : "deactivate")} teacher right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["TeachersSuccess"] = activate
            ? "Teacher activated successfully."
            : "Teacher deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }
}