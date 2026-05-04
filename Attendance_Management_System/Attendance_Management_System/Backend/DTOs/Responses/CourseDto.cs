using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.DTOs.Responses;

public class CourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EducationLevel EducationLevel { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}