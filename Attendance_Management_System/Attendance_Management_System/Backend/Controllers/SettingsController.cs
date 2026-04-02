using System.Security.Claims;
using Attendance_Management_System.Backend.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
[Route("settings")]
public class SettingsController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new SettingsIndexViewModel
        {
            Username = User.Identity?.Name ?? "-",
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "-",
            FullName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "-",
            Role = User.FindFirstValue(ClaimTypes.Role) ?? "-"
        };

        return View(model);
    }
}
