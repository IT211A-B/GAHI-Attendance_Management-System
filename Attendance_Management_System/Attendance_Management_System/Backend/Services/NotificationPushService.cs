using Attendance_Management_System.Backend.Constants;
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
        return _hubContext.Clients
            .Group(NotificationHubChannels.BuildUserGroupName(userId))
            .SendAsync(NotificationHubChannels.NewEventName, notification);
    }

    public Task PushToRoleAsync(string role, NotificationPushDto notification)
    {
        return _hubContext.Clients
            .Group(NotificationHubChannels.BuildRoleGroupName(role))
            .SendAsync(NotificationHubChannels.NewEventName, notification);
    }
}
