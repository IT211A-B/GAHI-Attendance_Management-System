namespace Attendance_Management_System.Backend.DTOs.Responses;

public class NotificationPushDto
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
