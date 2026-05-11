using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
[Route("settings")]
public class SettingsController : Controller
{
    private readonly IUsersService _usersService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IUsersService usersService, ILogger<SettingsController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        try
        {
            var user = await _usersService.GetUserByIdAsync(userId.Value);
            var viewModel = new ViewModels.Settings.SettingsIndexViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email
            };
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings for user {UserId}.", userId.Value);
            TempData["SettingsError"] = "Unable to load profile settings right now.";
            return View(new ViewModels.Settings.SettingsIndexViewModel());
        }
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] DTOs.Requests.UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["SettingsError"] = "Please provide valid profile details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _usersService.UpdateProfileAsync(userId.Value, request);
            TempData["SettingsSuccess"] = "Profile updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile for user {UserId}.", userId.Value);
            TempData["SettingsError"] = "Unable to update profile right now.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword([FromForm] UpdatePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["SettingsError"] = "Please provide valid password details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _usersService.UpdateProfileAsync(userId.Value, new UpdateProfileRequest
            {
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName
            });
            TempData["SettingsSuccess"] = "Profile updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile for user {UserId}.", userId.Value);
            TempData["SettingsError"] = "Unable to update profile right now.";
        }

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}