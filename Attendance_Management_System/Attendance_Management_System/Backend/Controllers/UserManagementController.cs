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
        var result = await _usersService.GetAllUsersAsync();

        var viewModel = new UsersIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load users right now.";
            return View(viewModel);
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

        return View(viewModel);
    }
}