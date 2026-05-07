using System.Security.Claims;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Controllers;

[Route("attendance")]
public class AttendanceManagementController : Controller
{
    private readonly IAttendanceQrService _attendanceQrService;
    private readonly AttendanceQrSettings _attendanceQrSettings;

    public AttendanceManagementController(
        IAttendanceQrService attendanceQrService,
        IOptions<AttendanceQrSettings> attendanceQrSettings)
    {
        _attendanceQrService = attendanceQrService;
        _attendanceQrSettings = attendanceQrSettings.Value?.IsValid() == true
            ? attendanceQrSettings.Value
            : AttendanceQrSettings.Default;
    }

    [HttpGet("")]
    [Authorize(Policy = "AdminOrTeacher")]
    public IActionResult Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? date)
    {
        return RedirectToAction("Index", "SectionManagement", new
        {
            sectionId,
            scheduleId,
            attendanceDate = (date ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd")
        });
    }

    [HttpGet("qr")]
    [Authorize(Policy = "AdminOrTeacher")]
    public IActionResult Qr()
    {
        var viewModel = new AttendanceQrPageViewModel
        {
            LiveFeedPollSeconds = _attendanceQrSettings.LiveFeedPollSeconds,
            RefreshThresholdSeconds = _attendanceQrSettings.RefreshThresholdSeconds
        };

        return View(viewModel);
    }

    [HttpGet("qr/options/sections")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> SearchQrSections([FromQuery] string? q, [FromQuery] int take = 8)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.SearchSectionsAsync(userContext.UserId, userContext.Role, q, take));
    }

    [HttpGet("qr/options/subjects")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> SearchQrSubjects([FromQuery] int sectionId, [FromQuery] string? q, [FromQuery] int take = 8)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.SearchSubjectsAsync(userContext.UserId, userContext.Role, sectionId, q, take));
    }

    [HttpGet("qr/options/periods")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> SearchQrPeriods([FromQuery] int sectionId, [FromQuery] int subjectId, [FromQuery] string? q, [FromQuery] int take = 8)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.SearchPeriodsAsync(userContext.UserId, userContext.Role, sectionId, subjectId, q, take));
    }

    [HttpPost("qr/sessions")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrSessionMutation)]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQrSession([FromBody] CreateAttendanceQrSessionRequest request)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Request payload is required.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.CreateSessionAsync(userContext.UserId, userContext.Role, request));
    }

    [HttpPost("qr/sessions/{sessionId}/refresh")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrSessionMutation)]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshQrSession(string sessionId)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.RefreshSessionAsync(userContext.UserId, userContext.Role, sessionId));
    }

    [HttpPost("qr/sessions/{sessionId}/close")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrSessionMutation)]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseQrSession(string sessionId)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(
            () => _attendanceQrService.CloseSessionAsync(userContext.UserId, userContext.Role, sessionId),
            "QR session closed.");
    }

    [HttpGet("qr/sessions/{sessionId}/checkins")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrLiveFeed)]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> GetQrSessionCheckins(string sessionId)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.GetLiveFeedAsync(userContext.UserId, userContext.Role, sessionId));
    }

    [HttpPost("qr/checkins")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrCheckin)]
    [Authorize(Policy = "StudentOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQrCheckin([FromBody] SubmitAttendanceQrCheckinRequest request)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Unable to resolve current user context.");
        }

        if (request is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Request payload is required.");
        }

        return await ExecuteApiAsync(() => _attendanceQrService.SubmitCheckinAsync(userContext.UserId, userContext.Role, request));
    }

    [HttpGet("scan")]
    [Authorize(Policy = "StudentOnly")]
    public IActionResult Scan()
    {
        return View();
    }

    [HttpPost("mark")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public IActionResult Mark([Bind(Prefix = "MarkForm")] MarkAttendanceFormViewModel form)
    {
        TempData["SectionAttendanceError"] = "The standalone attendance page is retired. Use the section checklist to mark or correct attendance.";

        return RedirectToAction("Index", "SectionManagement", new
        {
            sectionId = form.SectionId,
            scheduleId = form.ScheduleId,
            attendanceDate = form.Date.ToString("yyyy-MM-dd")
        });
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

    private async Task<IActionResult> ExecuteApiAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var payload = await action();
            return Ok(payload);
        }
        catch (Exception ex)
        {
            return BuildProblemResult(ex);
        }
    }

    private async Task<IActionResult> ExecuteApiAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            return Ok(new { message = successMessage });
        }
        catch (Exception ex)
        {
            return BuildProblemResult(ex);
        }
    }

    private IActionResult BuildProblemResult(Exception ex)
    {
        var statusCode = ex switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => MapInvalidOperationStatusCode(ex.Message),
            _ => StatusCodes.Status500InternalServerError
        };

        var title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Server Error"
        };

        return Problem(
            statusCode: statusCode,
            title: title,
            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Request failed." : ex.Message);
    }

    private static int MapInvalidOperationStatusCode(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (message.Contains("already", StringComparison.OrdinalIgnoreCase)
            || message.Contains("inactive", StringComparison.OrdinalIgnoreCase)
            || message.Contains("conflict", StringComparison.OrdinalIgnoreCase)
            || message.Contains("outside", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
