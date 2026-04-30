using System.Security.Claims;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
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
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.SearchSectionsAsync(userContext.UserId, userContext.Role, q, take);
        return BuildApiResult(result);
    }

    [HttpGet("qr/options/subjects")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> SearchQrSubjects([FromQuery] int sectionId, [FromQuery] string? q, [FromQuery] int take = 8)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.SearchSubjectsAsync(userContext.UserId, userContext.Role, sectionId, q, take);
        return BuildApiResult(result);
    }

    [HttpGet("qr/options/periods")]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> SearchQrPeriods([FromQuery] int sectionId, [FromQuery] int subjectId, [FromQuery] string? q, [FromQuery] int take = 8)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.SearchPeriodsAsync(userContext.UserId, userContext.Role, sectionId, subjectId, q, take);
        return BuildApiResult(result);
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
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        if (request is null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                ErrorCodes.BadRequest,
                "Request payload is required."));
        }

        var result = await _attendanceQrService.CreateSessionAsync(userContext.UserId, userContext.Role, request);
        return BuildApiResult(result);
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
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.RefreshSessionAsync(userContext.UserId, userContext.Role, sessionId);
        return BuildApiResult(result);
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
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.CloseSessionAsync(userContext.UserId, userContext.Role, sessionId);
        return BuildApiResult(result);
    }

    [HttpGet("qr/sessions/{sessionId}/checkins")]
    [EnableRateLimiting(RateLimitingPolicyNames.QrLiveFeed)]
    [Authorize(Policy = "AdminOrTeacher")]
    public async Task<IActionResult> GetQrSessionCheckins(string sessionId)
    {
        var userContext = GetUserContext();
        if (!userContext.IsValid)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        var result = await _attendanceQrService.GetLiveFeedAsync(userContext.UserId, userContext.Role, sessionId);
        return BuildApiResult(result);
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
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                ErrorCodes.Unauthorized,
                "Unable to resolve current user context."));
        }

        if (request is null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                ErrorCodes.BadRequest,
                "Request payload is required."));
        }

        var result = await _attendanceQrService.SubmitCheckinAsync(userContext.UserId, userContext.Role, request);
        return BuildApiResult(result);
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

    private static IActionResult BuildApiResult<T>(ApiResponse<T> response)
    {
        if (response.Success)
        {
            return new OkObjectResult(response);
        }

        var statusCode = response.Error?.Code switch
        {
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            "ALREADY_CHECKED_IN" => StatusCodes.Status409Conflict,
            "SESSION_INACTIVE" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }
}
