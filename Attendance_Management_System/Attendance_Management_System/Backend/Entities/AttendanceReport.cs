using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Stores generated attendance reports for a section and academic period
public class AttendanceReport : EntityBase
{
    // The section this report covers
    public int SectionId { get; set; }

    // The academic year this report belongs to
    public int AcademicYearId { get; set; }

    // User ID of the admin who generated the report
    public int GeneratedBy { get; set; }

    // Timestamp when the report was generated
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    // Type of report: "daily", "weekly", "monthly", or "summary"
    public string ReportType { get; set; } = "daily";

    // JSON-encoded report data for storage and retrieval
    public string DataJson { get; set; } = string.Empty;

    // Navigation property to the section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    // Navigation property to the academic year
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    // Navigation property to the user who generated the report
    [ForeignKey(nameof(GeneratedBy))]
    public User? Generator { get; set; }
}