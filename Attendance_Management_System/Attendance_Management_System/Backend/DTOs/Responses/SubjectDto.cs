namespace Attendance_Management_System.Backend.DTOs.Responses;

public class SubjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public int Units { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}