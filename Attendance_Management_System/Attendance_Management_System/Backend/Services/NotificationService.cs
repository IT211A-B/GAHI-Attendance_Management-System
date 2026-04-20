using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class NotificationService : INotificationService
{
    private const int DefaultTake = 20;
    private const int MaxTake = 100;

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
        var notification = BuildNotification(
            recipientUserId,
            type,
            title,
            message,
            linkUrl,
            payloadJson);

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            await _pushService.PushToUserAsync(recipientUserId, NotificationDtoMapper.ToPushDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to push notification {NotificationId} to user {RecipientUserId}.", notification.Id, recipientUserId);
        }

        return notification;
    }

    public Task<List<Notification>> GetRecentAsync(int userId, int take = 20)
    {
        var safeTake = ClampTake(take);

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

    private static Notification BuildNotification(
        int recipientUserId,
        string type,
        string title,
        string message,
        string? linkUrl,
        string? payloadJson)
    {
        var normalizedType = NormalizeAndValidateType(type);

        return new Notification
        {
            RecipientUserId = recipientUserId,
            Type = normalizedType,
            Title = title?.Trim() ?? string.Empty,
            Message = message?.Trim() ?? string.Empty,
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson,
            IsRead = false
        };
    }

    private static string NormalizeAndValidateType(string type)
    {
        var normalizedType = NotificationTypes.Normalize(type);
        if (NotificationTypes.IsSupported(normalizedType))
        {
            return normalizedType;
        }

        throw new ArgumentOutOfRangeException(nameof(type), "Unsupported notification type.");
    }

    private static int ClampTake(int take)
    {
        if (take <= 0)
        {
            return DefaultTake;
        }

        return Math.Min(take, MaxTake);
    }
}
