using Attendance_Management_System.Backend.ViewModels.Frontend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
public class FrontendPlaceholdersController : Controller
{
    [HttpGet("audit-logs")]
    public IActionResult AuditLogs() => View("Placeholder", BuildModel("Audit Logs", "This frontend route is now server-rendered in C# MVC. Backend API endpoints for audit log listing are not yet available in this project.", "/audit-logs"));

    [HttpGet("business-rules")]
    public IActionResult BusinessRules() => View("Placeholder", BuildModel("Business Rules", "This frontend route is now server-rendered in C# MVC. Backend API endpoints for business rule management are not yet available in this project.", "/business-rules"));

    [HttpGet("gate-terminals")]
    public IActionResult GateTerminals() => View("Placeholder", BuildModel("Gate Terminals", "This frontend route is now server-rendered in C# MVC. Backend API endpoints for gate terminal management are not yet available in this project.", "/gate-terminals"));

    [HttpGet("staff")]
    public IActionResult Staff() => View("Placeholder", BuildModel("Staff", "This frontend route is now server-rendered in C# MVC. Dedicated staff service endpoints are not yet exposed in this project.", "/staff"));

    private static PlaceholderPageViewModel BuildModel(string title, string description, string routePath)
    {
        return new PlaceholderPageViewModel
        {
            Title = title,
            Description = description,
            RoutePath = routePath
        };
    }
}
