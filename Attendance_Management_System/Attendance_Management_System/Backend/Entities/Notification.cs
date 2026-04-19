using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Notification : EntityBase
{
    public int RecipientUserId { get; set; }

    // Allowed values: signup, enrollment, checkin.
    public string Type { get; set; } = "signup";

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public string? PayloadJson { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }
}
