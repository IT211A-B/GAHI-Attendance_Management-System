using System.Security.Claims;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AllRoles")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index([FromQuery] string? window, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Challenge();
        }

        var viewModel = await _dashboardService.BuildIndexViewModelAsync(userContext.UserId, userContext.Role, window, from, to);
        if (!string.IsNullOrWhiteSpace(viewModel.ErrorMessage)
            && viewModel.Student is null
            && viewModel.Teacher is null
            && viewModel.Admin is null)
        {
            return Forbid();
        }

        return View(viewModel);
    }

    private (bool IsValid, int UserId, string Role) GetUserContext()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return (false, 0, string.Empty);
        }

        return (true, userId, role);
    }
}