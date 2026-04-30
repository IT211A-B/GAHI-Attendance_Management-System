using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a successful student check-in against a QR attendance session.
public class AttendanceQrCheckin : EntityBase
{
    // FK to owning QR session row.
    public int AttendanceQrSessionId { get; set; }

    // Student who checked in.
    public int StudentId { get; set; }

    // UTC time when check-in was accepted.
    public DateTimeOffset CheckedInAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Canonical status value: present or late.
    public string Status { get; set; } = "present";

    // Optional FK to attendance record created/updated by this check-in.
    public int? AttendanceId { get; set; }

    [ForeignKey(nameof(AttendanceQrSessionId))]
    public AttendanceQrSession? Session { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(AttendanceId))]
    public Attendance? Attendance { get; set; }
}
