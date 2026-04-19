using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Hubs;
using Attendance_Management_System.Backend.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Attendance_Management_System.Backend.Services;

public class NotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationPushService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushToUserAsync(int userId, NotificationPushDto notification)
    {
        return _hubContext.Clients.Group($"user:{userId}").SendAsync("notification:new", notification);
    }

    public Task PushToRoleAsync(string role, NotificationPushDto notification)
    {
        var normalizedRole = role?.Trim().ToLowerInvariant() ?? string.Empty;
        return _hubContext.Clients.Group($"role:{normalizedRole}").SendAsync("notification:new", notification);
    }
}
