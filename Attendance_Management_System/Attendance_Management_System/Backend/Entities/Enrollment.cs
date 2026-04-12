using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a student's enrollment in a section for an academic year
public class Enrollment : EntityBase
{
    // The student being enrolled
    public int StudentId { get; set; }

    // The section the student is enrolling in
    public int SectionId { get; set; }

    // The academic year for this enrollment
    public int AcademicYearId { get; set; }

    // Status: "pending", "approved", "rejected", or "dropped"
    public string Status { get; set; } = "pending";

    // Timestamp when the student dropped the enrollment (if applicable)
    public DateTimeOffset? DroppedAt { get; set; }

    // Timestamp when the enrollment was processed (approved/rejected)
    public DateTimeOffset? ProcessedAt { get; set; }

    // Admin user who processed the enrollment
    public int? ProcessedBy { get; set; }

    // Reason for rejection if the enrollment was rejected
    public string? RejectionReason { get; set; }

    // Warning flag for capacity-related warnings
    public bool HasWarning { get; set; } = false;

    // Warning message when enrolled in section near or over capacity
    public string? WarningMessage { get; set; }

    // Navigation property to the student
    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    // Navigation property to the section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    // Navigation property to the academic year
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    // Navigation property to the admin who processed the enrollment
    [ForeignKey(nameof(ProcessedBy))]
    public User? Processor { get; set; }
}