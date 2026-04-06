using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
[Route("settings")]
public class SettingsController : Controller
{
    private readonly IUsersService _usersService;

    public SettingsController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await BuildSettingsViewModelAsync(userId.Value);
        return View(model);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "ProfileForm")] UpdateProfileFormViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await BuildSettingsViewModelAsync(userId.Value);
        model.ProfileForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        var request = new UpdateProfileRequest
        {
            FirstName = NormalizeOptional(form.FirstName),
            MiddleName = NormalizeOptional(form.MiddleName),
            LastName = NormalizeOptional(form.LastName)
        };

        var result = await _usersService.UpdateProfileAsync(userId.Value, request);
        if (!result.Success)
        {
            ModelState.AddModelError("ProfileForm.FirstName", result.Error?.Message ?? "Unable to update profile right now.");
            return View(nameof(Index), model);
        }

        TempData["SettingsSuccess"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordFormViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await BuildSettingsViewModelAsync(userId.Value);
        model.PasswordForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        var request = new UpdateProfileRequest
        {
            CurrentPassword = form.CurrentPassword,
            NewPassword = form.NewPassword
        };

        var result = await _usersService.UpdateProfileAsync(userId.Value, request);
        if (!result.Success)
        {
            ModelState.AddModelError("PasswordForm.CurrentPassword", result.Error?.Message ?? "Unable to change password right now.");
            return View(nameof(Index), model);
        }

        TempData["SettingsSuccess"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<SettingsIndexViewModel> BuildSettingsViewModelAsync(int userId)
    {
        var fallbackFullName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "-";
        var viewModel = new SettingsIndexViewModel
        {
            Username = User.Identity?.Name ?? "-",
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "-",
            FullName = fallbackFullName,
            Role = User.FindFirstValue(ClaimTypes.Role) ?? "-"
        };

        var userResult = await _usersService.GetUserByIdAsync(userId);
        if (!userResult.Success || userResult.Data is null)
        {
            return viewModel;
        }

        var user = userResult.Data;

        viewModel.Email = string.IsNullOrWhiteSpace(user.Email) ? viewModel.Email : user.Email;
        viewModel.FullName = BuildFullName(user.FirstName, user.MiddleName, user.LastName, fallbackFullName);

        viewModel.ProfileForm = new UpdateProfileFormViewModel
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName
        };

        return viewModel;
    }

    private static string BuildFullName(string? firstName, string? middleName, string? lastName, string fallback)
    {
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? fallback : fullName;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
