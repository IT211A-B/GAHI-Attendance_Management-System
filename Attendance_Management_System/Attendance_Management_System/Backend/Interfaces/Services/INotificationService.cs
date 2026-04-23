using Attendance_Management_System.Backend.Entities;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface INotificationService
{
    Task<Notification> CreateAsync(int recipientUserId, string type, string title, string message, string? linkUrl = null, string? payloadJson = null);

    Task<List<Notification>> GetRecentAsync(int userId, int take = 20);

    Task<int> GetUnreadCountAsync(int userId);

    Task MarkReadAsync(int notificationId, int userId);

    Task MarkAllReadAsync(int userId);
}
