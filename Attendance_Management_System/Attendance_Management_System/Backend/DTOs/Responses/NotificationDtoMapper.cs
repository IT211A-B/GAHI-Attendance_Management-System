using Attendance_Management_System.Backend.Entities;

namespace Attendance_Management_System.Backend.DTOs.Responses;

public static class NotificationDtoMapper
{
    public static NotificationListItemDto ToListItemDto(Notification notification)
    {
        return new NotificationListItemDto
        {
            Id = notification.Id,
            RecipientUserId = notification.RecipientUserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            LinkUrl = notification.LinkUrl,
            IsRead = notification.IsRead,
            PayloadJson = notification.PayloadJson,
            CreatedAt = notification.CreatedAt
        };
    }

    public static NotificationPushDto ToPushDto(Notification notification)
    {
        return new NotificationPushDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            LinkUrl = notification.LinkUrl,
            CreatedAt = notification.CreatedAt
        };
    }
}
