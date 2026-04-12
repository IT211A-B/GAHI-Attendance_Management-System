using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Immutable history rows for attendance corrections and first-time marks.
public class AttendanceAudit : EntityBase
{
    public int AttendanceId { get; set; }

    // Allowed values: "created", "updated".
    public string Action { get; set; } = string.Empty;

    public TimeOnly? BeforeTimeIn { get; set; }
    public string? BeforeRemarks { get; set; }
    public string? BeforeStatus { get; set; }

    public TimeOnly? AfterTimeIn { get; set; }
    public string? AfterRemarks { get; set; }
    public string AfterStatus { get; set; } = string.Empty;

    public int ActorUserId { get; set; }
    public DateTimeOffset ActionAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(AttendanceId))]
    public Attendance? Attendance { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public User? ActorUser { get; set; }
}
