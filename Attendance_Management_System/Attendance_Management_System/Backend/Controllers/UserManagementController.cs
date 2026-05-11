using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("users")]
public class UserManagementController : Controller
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUsersService usersService, ILogger<UserManagementController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        UserIndexViewModel viewModel;

        try
        {
            var users = await _usersService.GetAllUsersAsync();
            viewModel = new UserIndexViewModel
            {
                Users = users
                    .Select(u => new UserListItemViewModel
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FullName = u.FullName,
                        Role = u.Role,
                        IsActive = u.IsActive
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users.");
            viewModel = new UserIndexViewModel
            {
                ErrorMessage = "Unable to load users right now."
            };
        }

        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["UsersError"] = "Please provide valid user details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _usersService.CreateUserAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            TempData["UsersError"] = "Unable to create user right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["UsersSuccess"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/toggle-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool isActive)
    {
        try
        {
            await _usersService.UpdateUserAsync(id, new UpdateUserRequest { IsActive = isActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle status for user {UserId}.", id);
            TempData["UsersError"] = $"Unable to {(isActive ? "activate" : "deactivate")} user right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["UsersSuccess"] = isActive
            ? "User activated successfully."
            : "User deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }
}