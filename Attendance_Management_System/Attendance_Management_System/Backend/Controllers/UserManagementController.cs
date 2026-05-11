using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
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
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateUserFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var request = new CreateUserRequest
        {
            Email = form.Email.Trim(),
            Password = form.Password,
            Role = form.Role.Trim().ToLowerInvariant()
        };

        try
        {
            await _usersService.CreateUserAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            ModelState.AddModelError("CreateForm.Email", "Unable to create user right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["UsersSuccess"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive)
    {
        try
        {
            await _usersService.UpdateUserAsync(id, new UpdateUserRequest { IsActive = isActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set status for user {UserId}.", id);
            TempData["UsersError"] = "Unable to update user status.";
            return RedirectToAction(nameof(Index));
        }

        TempData["UsersSuccess"] = isActive
            ? "User activated successfully."
            : "User deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<UsersIndexViewModel> BuildIndexViewModelAsync()
    {
        var viewModel = new UsersIndexViewModel();

        List<UserDto> users;

        try
        {
            users = await _usersService.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users.");
            viewModel.ErrorMessage = "Unable to load users right now.";
            return viewModel;
        }

        viewModel.Users = users
            .OrderBy(u => u.Email)
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            })
            .ToList();

        return viewModel;
    }
}