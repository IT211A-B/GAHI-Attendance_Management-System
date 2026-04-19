using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class NotificationService : INotificationService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "signup",
        "enrollment",
        "checkin"
    };

    private readonly AppDbContext _context;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        INotificationPushService pushService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(
        int recipientUserId,
        string type,
        string title,
        string message,
        string? linkUrl = null,
        string? payloadJson = null)
    {
        var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedTypes.Contains(normalizedType))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Unsupported notification type.");
        }

        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = normalizedType,
            Title = title?.Trim() ?? string.Empty,
            Message = message?.Trim() ?? string.Empty,
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            await _pushService.PushToUserAsync(recipientUserId, new NotificationPushDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                LinkUrl = notification.LinkUrl,
                CreatedAt = notification.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to push notification {NotificationId} to user {RecipientUserId}.", notification.Id, recipientUserId);
        }

        return notification;
    }

    public Task<List<Notification>> GetRecentAsync(int userId, int take = 20)
    {
        var safeTake = take <= 0 ? 20 : Math.Min(take, 100);

        return _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(safeTake)
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(int userId)
    {
        return _context.Notifications
            .AsNoTracking()
            .CountAsync(notification => notification.RecipientUserId == userId && !notification.IsRead);
    }

    public async Task MarkReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(item => item.Id == notificationId && item.RecipientUserId == userId);

        if (notification == null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        await _context.Notifications
            .Where(notification => notification.RecipientUserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(update => update
                .SetProperty(notification => notification.IsRead, _ => true));
    }
}
