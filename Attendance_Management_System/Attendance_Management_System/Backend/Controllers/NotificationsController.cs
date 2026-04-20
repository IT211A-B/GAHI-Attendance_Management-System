using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var notifications = await _notificationService.GetRecentAsync(userId.Value, take);
        var response = notifications.Select(NotificationDtoMapper.ToListItemDto).ToList();

        return Json(response);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await _notificationService.MarkReadAsync(id, userId.Value);
        return Json(new { success = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await _notificationService.MarkAllReadAsync(userId.Value);
        return Json(new { success = true });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
