namespace Attendance_Management_System.Backend.Entities;

public class Course : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}