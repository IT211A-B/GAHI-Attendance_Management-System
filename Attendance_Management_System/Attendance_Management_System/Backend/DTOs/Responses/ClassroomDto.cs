namespace Attendance_Management_System.Backend.DTOs.Responses;

public class ClassroomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}