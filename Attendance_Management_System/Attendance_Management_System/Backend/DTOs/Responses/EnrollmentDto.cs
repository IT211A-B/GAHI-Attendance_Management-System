namespace Attendance_Management_System.Backend.DTOs.Responses;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentNumber { get; set; }
    public string? StudentName { get; set; }
    public int? StudentCourseId { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public int AcademicYearId { get; set; }
    public string? AcademicYearLabel { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int? ProcessedBy { get; set; }
    public string? ProcessorName { get; set; }
    public string? RejectionReason { get; set; }
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }
}
