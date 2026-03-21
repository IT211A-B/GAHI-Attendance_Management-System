using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class AttendanceReport : EntityBase
{
    public int SectionId { get; set; }
    public int AcademicYearId { get; set; }
    public int GeneratedBy { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ReportType { get; set; } = "daily";
    public string DataJson { get; set; } = string.Empty;

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    [ForeignKey(nameof(GeneratedBy))]
    public User? Generator { get; set; }
}