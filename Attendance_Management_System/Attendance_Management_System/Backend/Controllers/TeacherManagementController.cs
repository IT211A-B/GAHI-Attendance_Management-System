using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("teachers")]
public class TeacherManagementController : Controller
{
    private readonly IAuthService _authService;
    private readonly ITeachersService _teachersService;

    public TeacherManagementController(IAuthService authService, ITeachersService teachersService)
    {
        _authService = authService;
        _teachersService = teachersService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount([Bind(Prefix = "CreateForm")] CreateTeacherAccountFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var request = new TeacherRegisterRequest
        {
            Email = form.Email.Trim(),
            Password = form.Password,
            ConfirmPassword = form.ConfirmPassword,
            FirstName = form.FirstName.Trim(),
            MiddleName = NormalizeOptional(form.MiddleName),
            LastName = form.LastName.Trim(),
            Department = form.Department.Trim(),
            Specialization = NormalizeOptional(form.Specialization)
        };

        var result = await _authService.RegisterTeacherAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Email", result.Message ?? "Unable to create teacher account right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["TeachersSuccess"] = "Teacher account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive)
    {
        var result = isActive
            ? await _teachersService.ActivateTeacherAsync(id)
            : await _teachersService.DeactivateTeacherAsync(id);

        if (!result.Success)
        {
            TempData["TeachersError"] = result.Error?.Message ?? "Unable to update teacher status.";
            return RedirectToAction(nameof(Index));
        }

        TempData["TeachersSuccess"] = isActive
            ? "Teacher activated successfully."
            : "Teacher deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<TeachersIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _teachersService.GetAllTeachersWithSectionsAsync();

        var viewModel = new TeachersIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load teachers right now.";
            return viewModel;
        }

        viewModel.Teachers = result.Data
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(teacher => new TeacherListItemViewModel
            {
                Id = teacher.Id,
                Name = string.Join(", ", new[] { teacher.LastName, teacher.FirstName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                Email = teacher.Email,
                Department = teacher.Department,
                EmployeeNumber = teacher.EmployeeNumber,
                IsActive = teacher.IsActive,
                SectionsText = teacher.Sections.Count == 0
                    ? "-"
                    : string.Join(", ", teacher.Sections.Select(s => s.SectionName))
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