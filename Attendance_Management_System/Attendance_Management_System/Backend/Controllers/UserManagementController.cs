using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("users")]
public class UserManagementController : Controller
{
    private readonly IUsersService _usersService;

    public UserManagementController(IUsersService usersService)
    {
        _usersService = usersService;
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

        var result = await _usersService.CreateUserAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Email", result.Error?.Message ?? "Unable to create user right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["UsersSuccess"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive)
    {
        var result = await _usersService.UpdateUserAsync(id, new UpdateUserRequest { IsActive = isActive });

        if (!result.Success)
        {
            TempData["UsersError"] = result.Error?.Message ?? "Unable to update user status.";
            return RedirectToAction(nameof(Index));
        }

        TempData["UsersSuccess"] = isActive
            ? "User activated successfully."
            : "User deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<UsersIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _usersService.GetAllUsersAsync();

        var viewModel = new UsersIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load users right now.";
            return viewModel;
        }

        viewModel.Users = result.Data
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