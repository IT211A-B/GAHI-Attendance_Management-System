using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface INotificationPushService
{
    Task PushToUserAsync(int userId, NotificationPushDto notification);

    Task PushToRoleAsync(string role, NotificationPushDto notification);
}
