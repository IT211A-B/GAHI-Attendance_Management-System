using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a short-lived teacher-owned QR attendance session.
public class AttendanceQrSession : EntityBase
{
    // Public session identifier exposed to clients.
    public string SessionId { get; set; } = string.Empty;

    // Scope: section tied to this QR session.
    public int SectionId { get; set; }

    // Scope: schedule tied to this QR session.
    public int ScheduleId { get; set; }

    // Scope: subject tied to this QR session.
    public int SubjectId { get; set; }

    // User account that created this session (teacher/admin account id).
    public int CreatedByUserId { get; set; }

    // Teacher owner id used for strict QR ownership checks.
    public int OwnerTeacherId { get; set; }

    // Session issue timestamp.
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Session expiration timestamp.
    public DateTimeOffset ExpiresAtUtc { get; set; }

    // Active flag; false when manually closed or invalidated.
    public bool IsActive { get; set; } = true;

    // Current nonce bound to the signed token payload.
    public string TokenNonce { get; set; } = string.Empty;

    // Optional close timestamp when session is deactivated.
    public DateTimeOffset? ClosedAtUtc { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public Schedule? Schedule { get; set; }

    [ForeignKey(nameof(SubjectId))]
    public Subject? Subject { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? Creator { get; set; }

    public ICollection<AttendanceQrCheckin> Checkins { get; set; } = new List<AttendanceQrCheckin>();
}
