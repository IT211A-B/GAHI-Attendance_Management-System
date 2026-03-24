namespace Attendance_Management_System.Backend.DTOs.Responses;

public class SectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public int AcademicYearId { get; set; }
    public string? AcademicYearLabel { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public int CurrentEnrollmentCount { get; set; }
    public string? CapacityStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
