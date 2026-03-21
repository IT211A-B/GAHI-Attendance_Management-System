using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Enrollment : EntityBase
{
    public int StudentId { get; set; }
    public int SectionId { get; set; }
    public int AcademicYearId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset? DroppedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int? ProcessedBy { get; set; }
    public string? RejectionReason { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    [ForeignKey(nameof(ProcessedBy))]
    public User? Processor { get; set; }
}
